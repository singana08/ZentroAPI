# Category API Implementation Guide

## Overview
The Category API provides endpoints to retrieve service categories and their associated subcategories in a hierarchical JSON structure. All subcategories are filtered by active status and ordered alphabetically by name.

---

## API Endpoints

### 1. Get All Categories with Subcategories
**Endpoint:** `GET /api/category/with-subcategories`

**Description:** Retrieves all categories with their active subcategories ordered by name.

**Authentication:** Not required (Anonymous)

**Response Status:** 
- `200 OK` - Successfully retrieved categories
- `500 Internal Server Error` - Server error

**Response Body:**
```json
{
  "success": true,
  "message": "Successfully retrieved 5 categories",
  "data": [
    {
      "id": 1,
      "name": "Cleaning Services",
      "description": "Professional cleaning and maintenance services",
      "icon": "🧹",
      "isActive": true,
      "createdAt": "2024-10-31T13:30:54Z",
      "updatedAt": null,
      "subcategories": [
        {
          "id": 1,
          "name": "Deep Cleaning",
          "description": "Comprehensive deep cleaning service",
          "icon": "✨",
          "isActive": true,
          "createdAt": "2024-10-31T13:30:54Z",
          "updatedAt": null
        },
        {
          "id": 2,
          "name": "Office Cleaning",
          "description": "Commercial office cleaning",
          "icon": "🏢",
          "isActive": true,
          "createdAt": "2024-10-31T13:30:54Z",
          "updatedAt": null
        }
      ]
    },
    {
      "id": 2,
      "name": "Home Repair",
      "description": "Home maintenance and repair services",
      "icon": "🔧",
      "isActive": true,
      "createdAt": "2024-10-31T13:30:54Z",
      "updatedAt": null,
      "subcategories": [
        {
          "id": 5,
          "name": "Electrical Repair",
          "description": "Electrical system repairs and installation",
          "icon": "⚡",
          "isActive": true,
          "createdAt": "2024-10-31T13:30:54Z",
          "updatedAt": null
        }
      ]
    }
  ]
}
```

---

### 2. Get Single Category with Subcategories
**Endpoint:** `GET /api/category/{categoryId}/with-subcategories`

**Description:** Retrieves a specific category with all its active subcategories.

**Parameters:**
- `categoryId` (path, required): The ID of the category (must be > 0)

**Authentication:** Not required (Anonymous)

**Response Status:** 
- `200 OK` - Successfully retrieved category
- `400 Bad Request` - Invalid category ID
- `404 Not Found` - Category not found
- `500 Internal Server Error` - Server error

**Success Response Body:**
```json
{
  "id": 1,
  "name": "Cleaning Services",
  "description": "Professional cleaning and maintenance services",
  "icon": "🧹",
  "isActive": true,
  "createdAt": "2024-10-31T13:30:54Z",
  "updatedAt": null,
  "subcategories": [
    {
      "id": 1,
      "name": "Deep Cleaning",
      "description": "Comprehensive deep cleaning service",
      "icon": "✨",
      "isActive": true,
      "createdAt": "2024-10-31T13:30:54Z",
      "updatedAt": null
    },
    {
      "id": 2,
      "name": "Office Cleaning",
      "description": "Commercial office cleaning",
      "icon": "🏢",
      "isActive": true,
      "createdAt": "2024-10-31T13:30:54Z",
      "updatedAt": null
    }
  ]
}
```

**Error Response Body (404):**
```json
{
  "success": false,
  "message": "Category with ID 999 not found"
}
```

**Error Response Body (400):**
```json
{
  "success": false,
  "message": "Category ID must be greater than 0"
}
```

---

## Features

### ✅ Active Status Filtering
- Only **active subcategories** (where `isActive = true`) are included in responses
- Categories themselves are included regardless of active status
- This ensures only available services are displayed to users

### ✅ Alphabetical Ordering
- **Categories** are ordered by name (A-Z)
- **Subcategories** within each category are ordered by name (A-Z)
- Consistent ordering across requests

### ✅ Hierarchical JSON Structure
- Categories contain nested array of subcategories
- Full category and subcategory details included
- Timestamps for audit trail

### ✅ Error Handling
- Comprehensive error messages
- Proper HTTP status codes
- Logging for debugging

---

## Database Schema

### master_category table
```sql
CREATE TABLE halulu_api.master_category (
    Id INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(500),
    Icon VARCHAR(255),
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    UpdatedAt TIMESTAMP WITH TIME ZONE,
    INDEX: IX_master_category_Name (unique),
    INDEX: IX_master_category_IsActive
);
```

### master_subcategory table
```sql
CREATE TABLE halulu_api.master_subcategory (
    Id INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    Icon VARCHAR(255),
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CategoryId INTEGER NOT NULL REFERENCES master_category(Id) ON DELETE CASCADE,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    UpdatedAt TIMESTAMP WITH TIME ZONE,
    UNIQUE(CategoryId, Name),
    INDEX: IX_master_subcategory_IsActive,
    FOREIGN KEY: FK_master_subcategory_master_category_CategoryId
);
```

---

## Implementation Details

### Entity Framework Core Models

#### Category Model
```csharp
[Table("master_category")]
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(255)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = [];
}
```

#### Subcategory Model
```csharp
[Table("master_subcategory")]
public class Subcategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(255)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
}
```

### Service Layer
- **ICategoryService**: Interface defining category operations
- **CategoryService**: Implementation with database queries using EF Core
- Async/await pattern for non-blocking operations
- Comprehensive error logging

### DTOs (Data Transfer Objects)
- **CategoryWithSubcategoriesDto**: Complete category with subcategories
- **SubcategoryDto**: Individual subcategory details
- **CategoryApiResponse**: API response wrapper with metadata

### Controller
- **CategoryController**: Handles HTTP requests
- Two public endpoints
- Proper exception handling and logging
- Swagger documentation included

---

## Usage Examples

### cURL Example
```bash
# Get all categories
curl -X GET "http://localhost:5000/api/category/with-subcategories" \
  -H "accept: application/json"

# Get single category
curl -X GET "http://localhost:5000/api/category/1/with-subcategories" \
  -H "accept: application/json"
```

### JavaScript/TypeScript Example
```typescript
// Get all categories
const response = await fetch('http://localhost:5000/api/category/with-subcategories');
const data = await response.json();

// Get single category
const categoryResponse = await fetch('http://localhost:5000/api/category/1/with-subcategories');
const categoryData = await categoryResponse.json();
```

### C# Example
```csharp
using HttpClient client = new HttpClient();

// Get all categories
var response = await client.GetAsync("http://localhost:5000/api/category/with-subcategories");
var content = await response.Content.ReadAsAsync<CategoryApiResponse>();

// Get single category
var categoryResponse = await client.GetAsync("http://localhost:5000/api/category/1/with-subcategories");
var categoryData = await categoryResponse.Content.ReadAsAsync<CategoryWithSubcategoriesDto>();
```

---

## Notes

1. **Performance**: The endpoints use `AsNoTracking()` for read-only queries, improving performance
2. **Logging**: All operations are logged for debugging and monitoring
3. **CORS**: Endpoints are configured with CORS policy "AllowMobileApp"
4. **Authentication**: Currently open to anonymous users (can be changed with `[Authorize]` attribute)
5. **Filtering**: Subcategories are filtered at the application level for consistency
6. **Relationships**: Cascade delete is configured (deleting a category deletes its subcategories)

---

## Version
- Created: 2024-10-31
- API Version: v1
- Database: PostgreSQL 12+
- .NET Version: 8.0
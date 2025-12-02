using System;
using System.Collections.Generic;

namespace ZentroAPI.TempModels;

public partial class MasterCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MasterSubcategory> MasterSubcategories { get; set; } = new List<MasterSubcategory>();
}

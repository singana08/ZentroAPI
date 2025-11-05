using System;
using System.Collections.Generic;

namespace HaluluAPI.TempModels;

public partial class MasterSubcategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public int CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual MasterCategory Category { get; set; } = null!;
}

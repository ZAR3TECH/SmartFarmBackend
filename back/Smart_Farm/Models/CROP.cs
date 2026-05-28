#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Farm.Models;

[Table("CROP")]
public partial class CROP
{
    [Key]
    public int Cid { get; set; }

    public int? Pid { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string Notes { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Area_size { get; set; }

    public DateOnly? Start_date { get; set; }

    [StringLength(100)]
    public string Soil_type { get; set; }

    [StringLength(100)]
    public string Current_Stage { get; set; }

    public int? Uid { get; set; }

    public int? FarmId { get; set; }

    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal? Depletion_mm { get; set; }

    public DateOnly? LastBalanceDate { get; set; }

    // ───────────── NAVIGATION PROPERTIES ─────────────

    [ForeignKey("FarmId")]
    [InverseProperty("CROPs")]
    public virtual FARM FarmNavigation { get; set; }

    [ForeignKey("Uid")]
    [InverseProperty("CROPs")]
    public virtual USER UidNavigation { get; set; }

    [ForeignKey("Pid")]
    [InverseProperty("CROPs")]
    public virtual PLANT PidNavigation { get; set; }

    // ───────────── COLLECTION NAVIGATIONS ─────────────

    [InverseProperty("CidNavigation")]
    public virtual ICollection<CROP_WATER_BALANCE_LOG> WaterBalanceLogs { get; set; } = new List<CROP_WATER_BALANCE_LOG>();

    [InverseProperty("CidNavigation")]
    public virtual ICollection<AI_Diagnosis> AI_Diagnoses { get; set; } = new List<AI_Diagnosis>();

    [InverseProperty("CidNavigation")]
    public virtual ICollection<IRRIGATION_STAGE> IRRIGATION_STAGEs { get; set; } = new List<IRRIGATION_STAGE>();

    [InverseProperty("CidNavigation")]
    public virtual ICollection<IRRIGATION> IRRIGATIONs { get; set; } = new List<IRRIGATION>();

    // ✔ FIXED FERTILIZER RELATION
    public virtual ICollection<FERTILIZER> Frs { get; set; } = new List<FERTILIZER>();
}
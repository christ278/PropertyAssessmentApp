using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

public enum SmsDeliveryStatus
{
    Success = 1,
    Failed = 2,
    Pending = 3
}

[Table("SmsNotificationLogs")]
public class SmsNotificationLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Display(Name = "Recipient Mobile No.")]
    public string RecipientNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Recipient Name")]
    public string? RecipientName { get; set; }

    public Guid? OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public virtual Owner? Owner { get; set; }

    [Required]
    [MaxLength(1000)]
    [Display(Name = "Message Content")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Sender ID")]
    public string? SenderId { get; set; }

    [Display(Name = "Delivery Status")]
    public SmsDeliveryStatus Status { get; set; } = SmsDeliveryStatus.Success;

    [MaxLength(500)]
    [Display(Name = "Gateway Response")]
    public string? GatewayResponse { get; set; }

    [Display(Name = "Date & Time Sent")]
    public DateTime SentAt { get; set; } = DateTime.Now;

    [MaxLength(150)]
    [Display(Name = "Sent By")]
    public string? SentBy { get; set; }
}


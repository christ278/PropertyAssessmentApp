using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IOrderPaymentService
{
    IEnumerable<OrderPayment> GetAll(string? search = null, bool? isActive = null, string? category = null);
    OrderPayment? GetById(Guid id);
    void Add(OrderPayment item);
    void Update(OrderPayment item);
    void Delete(Guid id);

    int GetTotalCount();
    int GetActiveCount();
    decimal GetAverageFee();
    IEnumerable<string> GetCategories();
}

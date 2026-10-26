using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class OrderPaymentService : IOrderPaymentService
{
    private readonly List<OrderPayment> _records = new();

    public OrderPaymentService()
    {
        SeedSampleData();
    }

    private void SeedSampleData()
    {
        _records.AddRange(new[]
        {
            new OrderPayment
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                Code = "OP-101",
                Particular = "Tax Declaration Certification Fee (Per Copy)",
                UnitCost = 150.00m,
                Category = "Certifications",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-60),
                UpdatedDate = DateTime.Now.AddDays(-5)
            },
            new OrderPayment
            {
                Id = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
                Code = "OP-102",
                Particular = "Certified True Copy of FAAS Sheet",
                UnitCost = 200.00m,
                Category = "Certifications",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-55),
                UpdatedDate = DateTime.Now.AddDays(-2)
            },
            new OrderPayment
            {
                Id = Guid.Parse("e4b3d82f-2d64-4e9b-8912-1f48619bc301"),
                Code = "OP-201",
                Particular = "Ocular Physical Field Inspection Fee",
                UnitCost = 500.00m,
                Category = "Field Inspection",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-50),
                UpdatedDate = DateTime.Now.AddDays(-10)
            },
            new OrderPayment
            {
                Id = Guid.Parse("b19c8f30-8a71-4822-921d-93f240984c12"),
                Code = "OP-301",
                Particular = "Real Property Annotation & Mortgages Fee",
                UnitCost = 350.00m,
                Category = "Annotations",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-45),
                UpdatedDate = DateTime.Now.AddDays(-1)
            },
            new OrderPayment
            {
                Id = Guid.Parse("f62e7492-3c10-410a-b851-d419b480e653"),
                Code = "OP-401",
                Particular = "Certificate of Non-Improvement Fee",
                UnitCost = 100.00m,
                Category = "Certifications",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-30),
                UpdatedDate = DateTime.Now.AddDays(-3)
            },
            new OrderPayment
            {
                Id = Guid.Parse("d34b1109-1a22-421f-a312-88f1109bc404"),
                Code = "OP-501",
                Particular = "Subdivision / Consolidation Mapping Fee",
                UnitCost = 750.00m,
                Category = "Tax Mapping",
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-20),
                UpdatedDate = DateTime.Now.AddDays(-4)
            }
        });
    }

    public IEnumerable<OrderPayment> GetAll(string? search = null, bool? isActive = null, string? category = null)
    {
        var query = _records.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.Code.ToLower().Contains(term)
                                  || r.Particular.ToLower().Contains(term)
                                  || r.Category.ToLower().Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(r => r.Code);
    }

    public OrderPayment? GetById(Guid id)
    {
        return _records.FirstOrDefault(r => r.Id == id);
    }

    public void Add(OrderPayment item)
    {
        if (item.Id == Guid.Empty)
        {
            item.Id = Guid.NewGuid();
        }
        item.CreatedDate = DateTime.Now;
        item.UpdatedDate = DateTime.Now;
        _records.Add(item);
    }

    public void Update(OrderPayment item)
    {
        var index = _records.FindIndex(r => r.Id == item.Id);
        if (index != -1)
        {
            item.UpdatedDate = DateTime.Now;
            _records[index] = item;
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _records.Remove(item);
        }
    }

    public int GetTotalCount() => _records.Count;
    public int GetActiveCount() => _records.Count(r => r.IsActive);
    public decimal GetAverageFee() => _records.Any() ? Math.Round(_records.Average(r => r.UnitCost), 2) : 0m;
    public IEnumerable<string> GetCategories() => _records.Select(r => r.Category).Distinct().OrderBy(c => c);
}

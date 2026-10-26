using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class OwnerService : IOwnerService
{
    private readonly ApplicationDbContext _context;

    public OwnerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Owner> GetAll(string? search = null, OwnerType? type = null, bool? isActive = null)
    {
        IQueryable<Owner> query = _context.TaxpayerOwners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => (o.FirstName != null && o.FirstName.Contains(term))
                                  || (o.LastName != null && o.LastName.Contains(term))
                                  || (o.CorporateName != null && o.CorporateName.Contains(term))
                                  || (o.TaxIdentNo != null && o.TaxIdentNo.Contains(term))
                                  || (o.RptinNumber != null && o.RptinNumber.Contains(term))
                                  || (o.BarangayName != null && o.BarangayName.Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(o => o.OwnerType == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        return query.OrderBy(o => o.LastName).ThenBy(o => o.CorporateName).ToList();
    }

    public Owner? GetById(Guid id)
    {
        return _context.TaxpayerOwners.Find(id);
    }

    public Owner? GetByRptin(string rptin)
    {
        if (string.IsNullOrWhiteSpace(rptin)) return null;
        var clean = rptin.Trim();
        return _context.TaxpayerOwners.AsNoTracking()
            .FirstOrDefault(o => o.RptinNumber == clean);
    }

    public void Add(Owner owner)
    {
        if (owner.Id == Guid.Empty)
        {
            owner.Id = Guid.NewGuid();
        }
        owner.CreatedDate = DateTime.Now;
        owner.UpdatedDate = DateTime.Now;

        var existing = _context.TaxpayerOwners.Find(owner.Id);
        if (existing == null)
        {
            _context.TaxpayerOwners.Add(owner);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(owner);
        }

        _context.SaveChanges();
    }

    public void Update(Owner owner)
    {
        var existing = _context.TaxpayerOwners.Find(owner.Id);
        if (existing != null)
        {
            owner.UpdatedDate = DateTime.Now;
            _context.Entry(existing).CurrentValues.SetValues(owner);
            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = _context.TaxpayerOwners.Find(id);
        if (item != null)
        {
            _context.TaxpayerOwners.Remove(item);
            _context.SaveChanges();
        }
    }

    public int GetTotalCount()
    {
        try { return _context.TaxpayerOwners.AsNoTracking().Count(); } catch { return 0; }
    }

    public int GetIndividualCount()
    {
        try { return _context.TaxpayerOwners.AsNoTracking().Count(o => o.OwnerType == OwnerType.Individual); } catch { return 0; }
    }

    public int GetCorporateCount()
    {
        try { return _context.TaxpayerOwners.AsNoTracking().Count(o => o.OwnerType == OwnerType.Corporate || o.OwnerType == OwnerType.Government); } catch { return 0; }
    }
}

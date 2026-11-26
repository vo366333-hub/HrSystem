using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Models;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ManagerName { get; private set; }
    public string Description { get; private set; }

    public Department(string name, string managerName, string description = "", Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва підрозділу не може бути порожньою.", nameof(name));

        if (string.IsNullOrWhiteSpace(managerName))
            throw new ArgumentException("Потрібно вказати керівника підрозділу.", nameof(managerName));

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        ManagerName = managerName.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public void UpdateDetails(string name, string managerName, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва підрозділу не може бути порожньою.", nameof(name));
        if (string.IsNullOrWhiteSpace(managerName))
            throw new ArgumentException("Потрібно вказати керівника підрозділу.", nameof(managerName));

        Name = name.Trim();
        ManagerName = managerName.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public override string ToString() => $"{Name} (кер. {ManagerName})";
}
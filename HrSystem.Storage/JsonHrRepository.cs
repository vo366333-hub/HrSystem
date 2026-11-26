using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Interfaces;
using HrSystem.Core.Models;
using HrSystem.Storage.Dto;

namespace HrSystem.Storage;

public class JsonHrRepository : IHrRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonHrRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Шлях до файлу даних не може бути порожнім.", nameof(filePath));

        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public HrData Load()
    {
        if (!File.Exists(_filePath))
            return EmptyData();

        try
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return EmptyData();

            var dto = JsonSerializer.Deserialize<HrStorageDto>(json, _jsonOptions);
            if (dto is null)
                return EmptyData();

            return new HrData(
                dto.Departments.Select(HrStorageMapper.FromDto).ToList(),
                dto.WorkSchedules.Select(HrStorageMapper.FromDto).ToList(),
                dto.Employees.Select(HrStorageMapper.FromDto).ToList(),
                dto.Requests.Select(HrStorageMapper.FromDto).ToList());
        }
        catch (JsonException ex)
        {
            throw new HrDomainException($"Файл даних пошкоджено або має некоректний формат JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            throw new HrDomainException($"Помилка читання файлу даних: {ex.Message}");
        }
    }

    public void Save(HrData data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var dto = new HrStorageDto
            {
                Departments = data.Departments.Select(HrStorageMapper.ToDto).ToList(),
                WorkSchedules = data.WorkSchedules.Select(HrStorageMapper.ToDto).ToList(),
                Employees = data.Employees.Select(HrStorageMapper.ToDto).ToList(),
                Requests = data.Requests.Select(HrStorageMapper.ToDto).ToList()
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (IOException ex)
        {
            throw new HrDomainException($"Не вдалося зберегти дані у файл: {ex.Message}");
        }
    }

    private static HrData EmptyData() =>
        new(new List<Department>(), new List<WorkSchedule>(), new List<Employee>(), new List<HrRequest>());
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Opilochka.Data;

/// <summary>
/// Шаблон сущностей
/// </summary>
public class DataItem
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    [Key] public long Id { get; set; }

    /// <summary>
    /// Флаг, указывающий, удалена ли запись
    /// </summary>
    [JsonIgnore]
    public bool IsDeleted { get; set; }
}
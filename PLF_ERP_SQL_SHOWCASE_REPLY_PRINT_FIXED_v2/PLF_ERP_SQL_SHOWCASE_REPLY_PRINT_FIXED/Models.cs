using System.Collections.Generic;
namespace PremiumLivingFurnitureWinForms;
public enum FieldKind{Text,Password,Integer,Decimal,Date,Combo,Lookup}
public class FieldDefinition{public string Column{get;set;}="";public string Label{get;set;}="";public FieldKind Kind{get;set;}=FieldKind.Text;public string[] Options{get;set;}=System.Array.Empty<string>();public string? LookupTable{get;set;}public object? DefaultValue{get;set;}public bool Required{get;set;}=true;public bool ReadOnly{get;set;}}
public record LookupItem(int Id,string Display){public override string ToString()=>Display;}
public record AutoCode(string TableName,string ColumnName,string Prefix,string Separator,int Width);
public record SalesOrderItemDraft(int ProductId,string Description,int Quantity,decimal UnitPrice,decimal Discount,decimal LineTotal);
public class ModuleDefinition{public string Title{get;}public string TableName{get;}public string Subtitle{get;}public List<FieldDefinition> Fields{get;}public ModuleDefinition(string title,string table,string subtitle,List<FieldDefinition> fields){Title=title;TableName=table;Subtitle=subtitle;Fields=fields;}}

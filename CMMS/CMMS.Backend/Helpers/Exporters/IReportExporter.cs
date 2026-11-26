namespace CMMS.Backend.Helpers.Exporters;

public interface IReportExporter
{
    string FormatName { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Export(string title, string subtitle, List<string> headers, List<List<string>> rows);
}

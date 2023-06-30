using System.Diagnostics.CodeAnalysis;

namespace XamarinFiles.FancyLogger
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class FancyLoggerOptions
    {
        public LinePrefix AllLines { get; set; } =
            new()
            {
                PrefixString = "LOG",
                // Prefix padding for consistent length between different instances
                PadLength = 0,
                PadString = " "
            };

        public LinePadding LongDividerLines { get; set; } =
            new()
            {
                PadLength = 70,
                PadString = "-"
            };

        public LinePadding ShortDividerLines { get; set; } =
            new()
            {
                PadLength = 30,
                PadString = "-"
            };

        public LinePadding SectionLines { get; set; } =
            new()
            {
                PadLength = 60,
                PadString = "#"
            };

        public LinePadding SubsectionLines { get; set; } =
            new()
            {
                PadLength = 50,
                PadString = "-"
            };

        public LinePadding HeaderLines { get; set; } =
            new()
            {
                PadLength = 40,
                PadString = "\\/"
            };

        public LinePadding FooterLines { get; set; } =
            new()
            {
                PadLength = 40,
                PadString = "/\\"
            };
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class LinePrefix : LinePadding
    {
        [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
        public string PrefixString { get; set; }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class LinePadding
    {
        public int PadLength { get; set; }

        public string PadString { get; set; }
    }
}

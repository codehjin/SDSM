namespace SDSM_App.Animations;

public class PlotItem
{
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;

    public int XCat { get; set; }
    public string XCatText { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Sum { get; set; }
    public int YVal1 { get; set; }
    public int YVal2 { get; set; }
    public int YVal3 { get; set; }
    public int YVal4 { get; set; }
    public int YVal5 { get; set; }
    public int YVal6 { get; set; }
    public int YVal7 { get; set; }
    public int YVal8 { get; set; }
    public int YVal9 { get; set; }
    public int YVal10 { get; set; }
    public int YVal11 { get; set; }
    public int YVal12 { get; set; }
    public int YVal13 { get; set; }
    public int YVal14 { get; set; }
    public int YVal15 { get; set; }
    public int YVal16 { get; set; }

    public double YRatio1 { get; set; }
    public double YRatio2 { get; set; }
    public double YRatio3 { get; set; }
    public double YRatio4 { get; set; }
    public double YRatio5 { get; set; }
    public double YRatio6 { get; set; }
    public double YRatio7 { get; set; }
    public double YRatio8 { get; set; }
    public double YRatio9 { get; set; }
    public double YRatio10 { get; set; }
    public double YRatio11 { get; set; }
    public double YRatio12 { get; set; }
    public double YRatio13 { get; set; }
    public double YRatio14 { get; set; }
    public double YRatio15 { get; set; }
    public double YRatio16 { get; set; }

    public string? YString1 { get; set; }
    public string? YString2 { get; set; }
    public string? YString3 { get; set; }
    public string? YString4 { get; set; }
    public string? YString5 { get; set; }
    public string? YString6 { get; set; }
    public string? YString7 { get; set; }
    public string? YString8 { get; set; }
    public string? YString9 { get; set; }
    public string? YString10 { get; set; }
    public string? YString11 { get; set; }
    public string? YString12 { get; set; }
    public string? YString13 { get; set; }
    public string? YString14 { get; set; }
    public string? YString15 { get; set; }
    public string? YString16 { get; set; }

    public List<PlotItem> SubItems { get; set; } = [];
}

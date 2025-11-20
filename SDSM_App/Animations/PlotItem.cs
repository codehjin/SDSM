namespace SDSM_App.Animations;

public class PlotItem
{
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
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
    public int YVal1a { get; set; }
    public int YVal1b { get; set; }
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
    public double YRatio1a { get; set; }
    public double YRatio1b { get; set; }

    public List<PlotItem> SubItems { get; set; } = [];
}

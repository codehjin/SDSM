namespace SDSM_App.Animations;

public class PlotItem
{
    public int XCat { get; set; }
    public string XCatText { get; set; } = string.Empty;
    public string XCatText2 { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public double YVal1 { get; set; }
    public double YVal2 { get; set; }
    public double YVal3 { get; set; }
    public double YVal4 { get; set; }
    public double YVal5 { get; set; }
    public double YVal6 { get; set; }
    public double YVal7 { get; set; }
    public double YVal8 { get; set; }
    public double YVal9 { get; set; }
    public double YVal10 { get; set; }
    public string YValText1 { get; set; } = string.Empty;
    public string YValText2 { get; set; } = string.Empty;
    public string YValText3 { get; set; } = string.Empty;

    public List<string> SubItems { get; set; } = [];
}

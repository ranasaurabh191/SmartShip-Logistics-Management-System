namespace SmartShip.ShipmentService.Models;

public class Package
{
    public int Id { get; set; }
    public double WeightKg { get; set; }
    public double LengthCm { get; set; }
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
}

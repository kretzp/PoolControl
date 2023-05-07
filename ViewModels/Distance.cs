using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Globalization;
using PoolControl.Helper;

namespace PoolControl.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public class Distance : MeasurementModelBase
{
    /// <summary>
    /// This Model will be used to hold the data for a Distance of a Water Box, and then it will be calculated to liters
    /// </summary>
    public Distance()
    {
        // Calculate Liter
        this.WhenAnyValue(ds => ds.Value, getLiter).ToPropertyEx(this, ds => ds.ValueL, deferSubscription: true);

        // Publish Liter
        this.WhenAnyValue(ds => ds.ValueL).Subscribe(_ => publishMessageValueL());

        // Create view
        this.WhenAnyValue(ds => ds.NameL, ds => ds.ValueL, ds => ds.ViewFormatL, ds => ds.UnitSignL, (_, temperature, viewFormat, unitSign) => $"{LocationNameL}: {temperature.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.FullTextL, deferSubscription: true);
        this.WhenAnyValue(ds => ds.NameL, ds => ds.ValueL, (_, _) => $"{LocationNameL}:").ToPropertyEx(this, ds => ds.LabelL, deferSubscription: true);
        this.WhenAnyValue(ds => ds.ValueL, ds => ds.ViewFormatL, ds => ds.UnitSignL, (value, viewFormat, unitSign) => $"{value.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.ValueWithUnitL, deferSubscription: true);
    }

    private double getLiter(double cm)
    {
        return 7.854 * (90 + 9.26 - cm * 0.95);
    }

    protected void publishMessageValueL()
    {
        publishMessageWithType(PoolControlHelper.GetPropertyName(() => ValueL), InterfaceFormatDecimalPointL);
    }

    [Reactive]
    [JsonProperty]
    public string? NameL { get; set; }

    public string LocationNameL
    {
        get
        {
            var ret = "Nix";
            try
            {
                if (NameL != null) ret = (string)typeof(Resource).GetProperty(NameL)?.GetValue(null)!;
            }
            catch (Exception)
            {
                // ignored
            }

            return ret;
        }
    }

    [Reactive]
    [JsonProperty]
    public string? UnitSignL { get; set; }

    [Reactive]
    [JsonProperty]
    public string? ViewFormatL { get; set; }

    [Reactive]
    [JsonProperty]
    public string? InterfaceFormatL { get; set; }

    [JsonIgnore]
    public string InterfaceFormatDecimalPointL => ValueL.ToString(InterfaceFormatL, new CultureInfo("en-US"));

    [JsonIgnore]
    [ObservableAsProperty]
    public double ValueL { get; }

    [JsonIgnore]
    [ObservableAsProperty]
    public string? FullTextL { get; }

    [JsonIgnore]
    [ObservableAsProperty]
    public string? LabelL { get; }

    [JsonIgnore]
    [ObservableAsProperty]
    public string? ValueWithUnitL { get; }

    [Reactive]
    [JsonProperty]
    public int NumberOfMeasurements { get; set; }

    [JsonIgnore]
    public int Trigger
    {
        get
        {
            var address = -1;
            try
            {
                address = int.Parse(Address?.Split('/')[0] ?? string.Empty);
            }
            catch (Exception)
            {
                // ignored
            }

            return address;
        }
    }

    [JsonIgnore]
    public int Echo
    {
        get
        {
            var address = -1;
            try
            {
                address = int.Parse(Address?.Split('/')[1] ?? string.Empty);
            }
            catch (Exception)
            {
                // ignored
            }

            return address;
        }
    }
}
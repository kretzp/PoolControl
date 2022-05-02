using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoolControl.Helper;

namespace PoolControl.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Distance : MeasurementModelBase
    {
        public Distance()
        {
            // Calculate Liter
            this.WhenAnyValue(ds => ds.Value, value => getLiter(value)).ToPropertyEx(this, ds => ds.ValueL, deferSubscription: true);

            // Publish Liter
            this.WhenAnyValue(ds => ds.ValueL).Subscribe(valuel => publishMessageValueL());

            // Create view
            this.WhenAnyValue(ds => ds.NameL, ds => ds.ValueL, ds => ds.ViewFormatL, ds => ds.UnitSignL, (name, temperature, viewFormat, unitSign) => $"{name}: {temperature.ToString(viewFormat)} {unitSign}").ToPropertyEx(this, ds => ds.FullTextL, deferSubscription: true);
            this.WhenAnyValue(ds => ds.NameL, ds => ds.ValueL, (name, value) => $"{name}:").ToPropertyEx(this, ds => ds.LabelL, deferSubscription: true);
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
        public string NameL { get; set; }

        [Reactive]
        [JsonProperty]
        public string UnitSignL { get; set; }

        [Reactive]
        [JsonProperty]
        public string ViewFormatL { get; set; }

        [Reactive]
        [JsonProperty]
        public string InterfaceFormatL { get; set; }

        [JsonIgnore]
        public string InterfaceFormatDecimalPointL
        {
            get { return ValueL.ToString(InterfaceFormatL, new CultureInfo("en-US")); }
        }

        [JsonIgnore]
        [ObservableAsProperty]
        public double ValueL { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string FullTextL { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string LabelL { get; }

        [JsonIgnore]
        [ObservableAsProperty]
        public string ValueWithUnitL { get; }

        [Reactive]
        [JsonProperty]
        public int NumberOfMeasurements { get; set; }

        [JsonIgnore]
        public int Trigger
        {
            get
            {
                int address = -1;
                try
                {
                    address = int.Parse(Address.Split('/')[0]);
                }
                catch (Exception)
                {
                }
                return address;
            }
        }

        [JsonIgnore]
        public int Echo
        {
            get
            {
                int address = -1;
                try
                {
                    address = int.Parse(Address.Split('/')[1]);
                }
                catch (Exception)
                {
                }
                return address;
            }
        }
    }
}

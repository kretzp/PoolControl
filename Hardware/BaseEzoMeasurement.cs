using System;
using System.Device.I2c;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PoolControl.Hardware
{
    public class BaseEzoMeasurement : BaseMeasurement
    {
        private const int BUFFER_SIZE = 16;
        private const int MILLIS_TO_WAIT = 1000;

        protected int I2CAddress
        {
            get
            {
                int address = -1;
                try
                {
                    address = int.Parse(ModelBase.Address);
                }
                catch(Exception)
                {
                }
                return address;
            }
        }

        public BaseEzoMeasurement()
        {
            Logger = Log.Logger.ForContext<BaseEzoMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
        }

        public MeasurementResult getDeviceInformation()
        {
            return send_i2c_command("i");
        }

        public MeasurementResult getDeviceStatus()
        {
            return send_i2c_command("Status");
        }

        public MeasurementResult getLedState()
        {
            return send_i2c_command("L,?");
        }

        public MeasurementResult setLedStateOn()
        {
            return send_i2c_command("L,1");
        }
        public MeasurementResult setLedStateOff()
        {
            return send_i2c_command("L,0");
        }

        public MeasurementResult switchLedState(bool state)
        {
            string sw = state ? "1" : "0";
            return send_i2c_command($"L,{sw}");
        }

        public MeasurementResult findDevice()
        {
            return send_i2c_command("Find");
        }

        public MeasurementResult terminateFind()
        {
            return getLedState();
        }

        public MeasurementResult takeReading()
        {
            return send_i2c_command("R");
        }

        public MeasurementResult sleep()
        {
            return send_i2c_command("Sleeps");
        }

        public MeasurementResult awake()
        {
            return getDeviceInformation();
        }

        public MeasurementResult factoryReset()
        {
            return send_i2c_command("Factory");
        }

        public MeasurementResult deviceCalibrated()
        {
            return send_i2c_command("Cal,?");
        }

        public MeasurementResult clearCalibration()
        {
            return send_i2c_command("Cal,clear");
        }

        public MeasurementResult setName(string name)
        {
            return send_i2c_command("Name," + name);
        }

        public MeasurementResult clearName()
        {
            return setName("");
        }

        public MeasurementResult getName()
        {
            return send_i2c_command("Name,?");
        }

        /*
         * Parts of this section has been used from
         * https://github.com/letscontrolit/ESPEasy
         * */
        public virtual MeasurementResult send_i2c_command(string command)
        {
            MeasurementResult ezoResult = new MeasurementResult { ReturnCode = (int)MeasurmentResultCode.PENDING, StatusInfo = "Error: ", Result = double.NaN, Device = $"{GetType().Name}@{I2CAddress}", Command = command };

            try
            {
                string resString = "";

                // When we Export the calibration, we have to fetch it multiple times
                bool export = "Export".Equals(command);

                do
                {
                    ezoResult.ReturnCode = (int)MeasurmentResultCode.PENDING;
                    Logger.Information("> cmd = " + command + " to Address " + I2CAddress);
                    ReadOnlySpan<byte> writeBuffer = new(Encoding.ASCII.GetBytes(command));

                    using (I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, I2CAddress)))
                    {
                        i2c.Write(writeBuffer);
                        // don't read answer if we want to go to sleep
                        if (command.Length > 4 && command.Substring(0, 5).ToLower().Equals("sleep"))
                        {
                            ezoResult.ReturnCode = 1;
                            ezoResult.StatusInfo = "Going to sleep";
                            return ezoResult;
                        }


                        Logger.Debug($"  Waiting {MILLIS_TO_WAIT} before fetching answer");
                        Thread.Sleep(MILLIS_TO_WAIT);

                        while (ezoResult.ReturnCode == (int)MeasurmentResultCode.PENDING)
                        {
                            Span<byte> readBuffer = new(new byte[BUFFER_SIZE]) { };
                            i2c.Read(readBuffer);

                            ezoResult.ReturnCode = readBuffer[0];

                            switch (ezoResult.ReturnCode)
                            {
                                case (int)MeasurmentResultCode.SUCCESS:
                                    Logger.Information("< success, answer = " + BitConverter.ToString(readBuffer.ToArray()));

                                    int nullByteIndexLength = readBuffer.IndexOf<byte>(0x00) - 1;
                                    if (nullByteIndexLength < 1)
                                    {
                                        Logger.Information("< success, without answer");
                                        ezoResult.Result = double.NaN;
                                        ezoResult.StatusInfo = "OK without return value";
                                    }
                                    else
                                    {
                                        string slice = Encoding.ASCII.GetString(readBuffer.Slice(1, nullByteIndexLength));

                                        Logger.Debug($"Slice: {slice}");

                                        if (export)
                                        {
                                            if ("*DONE".Equals(slice))
                                            {
                                                Logger.Debug("Done!!!");
                                                export = false;
                                            }
                                            else
                                            {
                                                resString += slice;
                                            }
                                        }
                                        else
                                        {
                                            resString = slice;
                                            try
                                            {
                                                ezoResult.Result = double.Parse(resString, new CultureInfo("en-US"));
                                            }
                                            catch (Exception)
                                            {
                                                if (resString.StartsWith("?L,") && resString.Length > 3)
                                                {
                                                    ezoResult.Result = double.Parse(resString.Substring(3), new CultureInfo("en-US"));
                                                }
                                                else if (resString.StartsWith("?Cal,") && resString.Length > 5)
                                                {
                                                    ezoResult.Result = double.Parse(resString.Substring(5), new CultureInfo("en-US"));
                                                }
                                                else if (resString.StartsWith("?Plock,") && resString.Length > 7)
                                                {
                                                    ezoResult.Result = double.Parse(resString.Substring(7), new CultureInfo("en-US"));
                                                }
                                            }
                                        }
                                        Logger.Information("< success, answer = " + resString);
                                        ezoResult.StatusInfo = $"OK:{resString}";
                                    }

                                    break;
                                case (int)MeasurmentResultCode.SYNTAXERROR:
                                    Logger.Information("< syntax error = " + BitConverter.ToString(readBuffer.ToArray()));
                                    ezoResult.StatusInfo += "syntax error";
                                    break;
                                case (int)MeasurmentResultCode.PENDING:
                                    Logger.Information("< command pending");
                                    break;
                                case (int)MeasurmentResultCode.NODATATOSEND:
                                    Logger.Information("< no data");
                                    break;
                                default:
                                    Logger.Information("< command failed = " + BitConverter.ToString(readBuffer.ToArray()));
                                    ezoResult.StatusInfo += "command failed!";
                                    break;
                            }
                        }
                    }
                } while (export);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in EzoResult send_i2c_command {command} Address {I2CAddress}");
                ezoResult.StatusInfo += ex.Message;
            }

            ezoResult.TimeStamp = DateTime.Now;

            return ezoResult;
        }
        public override MeasurementResult DoMeasurement()
        {
            return takeReading();
        }
    }
}

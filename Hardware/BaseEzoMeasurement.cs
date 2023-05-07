using PoolControl.ViewModels;
using System;
using System.Device.I2c;
using System.Globalization;
using System.Text;
using System.Threading;
using PoolControl.Helper;

namespace PoolControl.Hardware;

public class BaseEzoMeasurement : BaseMeasurement
{
    private const int BufferSize = 32;
    private const int MillisToWait = 1000;

    protected int I2CAddress
    {
        get
        {
            var address = -1;
            try
            {
                if (ModelBase?.Address != null) address = int.Parse(ModelBase.Address);
            }
            catch (Exception)
            {
                // ignored
            }

            return address;
        }
    }

    public BaseEzoMeasurement()
    {
        if (Log.Logger != null)
            Logger = Log.Logger.ForContext<BaseEzoMeasurement>() ?? throw new ArgumentNullException(nameof(Logger));
    }

    private MeasurementResult getDeviceInformation()
    {
        return send_i2c_command("i");
    }

    private MeasurementResult getDeviceStatus()
    {
        return send_i2c_command("Status");
    }

    private MeasurementResult getLedState()
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
        var sw = state ? "1" : "0";
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

    protected MeasurementResult takeReading()
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

    private MeasurementResult setName(string name)
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
    protected virtual MeasurementResult send_i2c_command(string command)
    {
        var ezoResult = new MeasurementResult { ReturnCode = (int)MeasurementResultCode.Pending, StatusInfo = "Error: ", Result = double.NaN, Device = $"{GetType().Name}@{I2CAddress}", Command = command };

        try
        {
            var resString = "";

            // When we Export the calibration, we have to fetch it multiple times
            var export = "Export".Equals(command);

            do
            {
                ezoResult.ReturnCode = (int)MeasurementResultCode.Pending;
                Logger?.Information("> cmd = {Command} to Address {I2CAddress}", command, I2CAddress);
                ReadOnlySpan<byte> writeBuffer = new(Encoding.ASCII.GetBytes(command));

                using var i2C = I2cDevice.Create(new I2cConnectionSettings(1, I2CAddress));
                i2C.Write(writeBuffer);
                // don't read answer if we want to go to sleep
                if (command.Length > 4 && command[..5].ToLower().Equals("sleep"))
                {
                    ezoResult.ReturnCode = 1;
                    ezoResult.StatusInfo = "Going to sleep";
                    return ezoResult;
                }


                Logger?.Debug("  Waiting {MillisToWait} before fetching answer", MillisToWait);
                Thread.Sleep(MillisToWait);

                while (ezoResult.ReturnCode == (int)MeasurementResultCode.Pending)
                {
                    Span<byte> readBuffer = new(new byte[BufferSize]);
                    i2C.Read(readBuffer);

                    ezoResult.ReturnCode = readBuffer[0];

                    switch (ezoResult.ReturnCode)
                    {
                        case (int)MeasurementResultCode.Success:
                            Logger?.Information("< success, answer = {Bits}", BitConverter.ToString(readBuffer.ToArray()));

                            var nullByteIndexLength = readBuffer.IndexOf<byte>(0x00) - 1;
                            if (nullByteIndexLength < 1)
                            {
                                Logger?.Information("< success, without answer");
                                ezoResult.Result = double.NaN;
                                ezoResult.StatusInfo = "OK without return value";
                            }
                            else
                            {
                                var slice = Encoding.ASCII.GetString(readBuffer.Slice(1, nullByteIndexLength));

                                Logger?.Debug("Slice: {Slice}", slice);

                                if (export)
                                {
                                    if ("*DONE".Equals(slice))
                                    {
                                        Logger?.Debug("Done!!!");
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
                                        if (resString.ToLower().StartsWith("?l,") && resString.Length > 3)
                                        {
                                            ezoResult.Result = double.Parse(resString[3..], new CultureInfo("en-US"));
                                        }
                                        else if (resString.ToLower().StartsWith("?cal,") && resString.Length > 5)
                                        {
                                            ezoResult.Result = double.Parse(resString[5..], new CultureInfo("en-US"));
                                        }
                                        else if (resString.ToLower().StartsWith("?pLock,") && resString.Length > 7)
                                        {
                                            ezoResult.Result = double.Parse(resString[7..], new CultureInfo("en-US"));
                                        }
                                        else if (resString.ToLower().StartsWith("?slope,") && resString.Length > 7)
                                        {
                                            resString = resString[7..];
                                        }
                                        else if (resString.ToLower().StartsWith("?status") && resString.Length > 7)
                                        {
                                            ezoResult.Result = double.Parse(resString.Split(",")[2], new CultureInfo("en-US"));
                                        }
                                    }
                                }
                                Logger?.Information("< success, answer = {ResString}", resString);
                                ezoResult.StatusInfo = resString;
                            }

                            break;
                        case (int)MeasurementResultCode.SyntaxError:
                            Logger?.Information("< syntax error = {Bits}", BitConverter.ToString(readBuffer.ToArray()));
                            ezoResult.StatusInfo += "syntax error";
                            break;
                        case (int)MeasurementResultCode.Pending:
                            Logger?.Information("< command pending");
                            break;
                        case (int)MeasurementResultCode.NoDataToSend:
                            Logger?.Information("< no data");
                            break;
                        default:
                            Logger?.Information("< command failed = {Bits}", BitConverter.ToString(readBuffer.ToArray()));
                            ezoResult.StatusInfo += "command failed!";
                            break;
                    }
                }
            } while (export);
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Error in EzoResult send_i2c_command {Command} Address {I2CAddress}",command, I2CAddress);
            ezoResult.StatusInfo += ex.Message;
        }

        ezoResult.TimeStamp = DateTime.Now;

        return ezoResult;
    }

    protected override MeasurementResult DoMeasurement()
    {
        var vmr = DoVoltageMeasurement();
        ((EzoBase)ModelBase!).Voltage = vmr.Result;

        return takeReading();
    }

    protected MeasurementResult DoVoltageMeasurement()
    {
        var mr = getDeviceStatus();
        if (mr.Result <= 0)
        {
            throw new ArgumentOutOfRangeException($"Error in {GetType().Name}  <= 0");
        }

        return mr;
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PoolControl.Helper
{
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public static class PropertySetter
    {
        private static bool setProperty(object parent, PropertyInfo myPropInfo, string value)
        {
            if (myPropInfo == null)
            {
                return false;
            }
            else
            {
                if (myPropInfo.PropertyType.Equals(typeof(decimal)))
                {
                    myPropInfo.SetValue(parent, decimal.Parse(value, new CultureInfo("en-US")));
                }
                else if (myPropInfo.PropertyType.Equals(typeof(int)))
                {
                    myPropInfo.SetValue(parent, int.Parse(value, new CultureInfo("en-US")));
                }
                else if (myPropInfo.PropertyType.Equals(typeof(double)))
                {
                    myPropInfo.SetValue(parent, double.Parse(value, new CultureInfo("en-US")));
                }
                else if (myPropInfo.PropertyType.Equals(typeof(float)))
                {
                    myPropInfo.SetValue(parent, float.Parse(value, new CultureInfo("en-US")));
                }
                else if (myPropInfo.PropertyType.Equals(typeof(string)))
                {
                    myPropInfo.SetValue(parent, value);
                }
                else if (myPropInfo.PropertyType.Equals(typeof(bool)))
                {
                    myPropInfo.SetValue(parent, !(value.StartsWith("0") || value.ToLower().StartsWith("fa") || value.ToLower().StartsWith("of")));
                }
                else
                {
                    return false;
                }

                return true;
            }
        }

        /*
         * baseObject
         *   |
         *   --- objectNameToSet[key]
         *            |
         *            ---propertyName = propertyValue
         * or
         * 
         * baseObject
         *   |
         *   ---propertyName = propertyValue
         *            
         */
        public static Result setProperty(object baseObject, string propertyName, string propertyValue, string objectNameToSet = "", string key = "")
        {
            string info = $"setting Object: {baseObject} Object2: {objectNameToSet} Key: {key} Property: {propertyName} Value: {propertyValue}";
            
            PropertyInfo childPropertyInfo = null;
            object propertyObject = null;

            try
            {
                if (string.IsNullOrEmpty(objectNameToSet))
                {
                    propertyObject = baseObject;
                }
                else
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        propertyObject = baseObject.GetType().GetProperty(objectNameToSet).GetValue(baseObject);
                    }
                    else
                    {
                        Dictionary<string, object> dict = (Dictionary<string, object>)baseObject.GetType().GetProperty(objectNameToSet + "Obj").GetValue(baseObject);

                        propertyObject = dict[key];
                    }

                    childPropertyInfo = propertyObject.GetType().GetProperty(propertyName);
                }

                if (!setProperty(propertyObject, childPropertyInfo, propertyValue))
                {
                    return new Result { Success = false, Message = $"Error while setting Object: {propertyObject} Object2: {objectNameToSet} Key: {key} Property: {childPropertyInfo.Name} Value: {propertyValue}" };
                }
            }
            catch (Exception ex)
            {
                return new Result { Success = false, Message = $"{ex.Message} while {info}" };
            }

            return new Result { Success = true, Message = "Success: {info}" };
        }
    }
}

using Confluent.Kafka;
using KafkaConsumer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer
{
    public static class Helper
    {
        public static string SanitiseString(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Replace(" ", "").Replace("-", "");
        }
        public static string ToHex(this string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char ch in input)
                stringBuilder.AppendFormat("{0:X2}", (int)ch);
            return stringBuilder.ToString().Trim();
        }
        public static byte[] HexStringToByteArray(this string hexinput)
        {
            if (hexinput == string.Empty)
                return (byte[])null;
            if (hexinput.Length % 2 == 1)
                hexinput = "0" + hexinput;
            int length = hexinput.Length / 2;
            byte[] numArray = new byte[length];
            for (int index = 0; index < length; ++index)
                numArray[index] = Convert.ToByte(hexinput.Substring(index * 2, 2), 16);
            return numArray;
        }
        public static string ConvertToNtsaFormat(this BCEMessage message)
        {
            string ns = "N";
            string ew = "E";
            int gpsStatus = Convert.ToInt16(!message.Valid);
            var dateTime = message.Event.TimeStamp.ConvertToDateTime().AddHours(3);
            if (message.Payload.Longitude < 0)
            {
                ew = "W";
            }
            if (message.Payload.Latitude < 0)
            {
                ns = "S";
            }
            //2023-05-11,11:14:50,000016100005024,0101011,KDG 832Y,0,00000.000,0,0,34.8881,,0.60288,,0,0,0
            string strData = $"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss},{message.Event.UniqueId},{message.Event.UniqueId},{message.Event.UniqueId},{message.Payload.SpeedGps},{message.Payload.OdometerGps},{gpsStatus},{message.Payload.SatellitesFix},{message.Payload.Longitude},{ns},{message.Payload.Latitude},{ew},0,0,{Convert.ToInt16(!message.Payload.Input5State)}#";
            return strData;
        }
        public static string ConvertToNtsaFormat(this SpeedLimiter message)
        {
            string ns = "N";
            string ew = "E";
            int gpsStatus = 0;
            var dateTime = message.GpsDateTime;
            if (message.Longitude < 0)
            {
                ew = "W";
            }
            if (message.Latitude < 0)
            {
                ns = "S";
            }
            //2023-05-11,11:14:50,000016100005024,0101011,KDG 832Y,0,00000.000,0,0,34.8881,,0.60288,,0,0,0
            string strData = $"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss},{message.DeviceId},{message.DeviceId},{message.DeviceId},{message.Speed},{message.Odometer},{gpsStatus},{message.Satellites},{message.Longitude},{ns},{message.Latitude},{ew},{Convert.ToInt16(!message.PowerSignal)},0,{Convert.ToInt16(!message.IgnitionStatus)}#";
            return strData;
        }

        public static string ConvertToNtsaFormat(this NtsaForwardData<SpeedLimiter> data)
        {
            var message = data.Data;
            string ns = "N";
            string ew = "E";
            int gpsStatus = 0;
            var dateTime = message.GpsDateTime;
            if (message.Longitude < 0)
            {
                ew = "W";
            }
            if (message.Latitude < 0)
            {
                ns = "S";
            }
            //2023-05-11,11:14:50,000016100005024,0101011,KDG 832Y,0,00000.000,0,0,34.8881,,0.60288,,0,0,0
            string strData = $"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss},{message.DeviceId},{message.DeviceId},{message.DeviceId},{message.Speed},{message.Odometer},{gpsStatus},{message.Satellites},{message.Longitude},{ns},{message.Latitude},{ew},{Convert.ToInt16(!message.PowerSignal)},0,{Convert.ToInt16(!message.IgnitionStatus)}#";
            return strData;
        }
        public static DateTime ConvertToDateTime(this int timestamp)
        {
            return new DateTime(1970, 1, 1).AddSeconds(timestamp);
        }
        public static SpeedLimiter ConvertToSpeedLimiter(this BCEMessage message)
        {
            return new SpeedLimiter
            {
                GpsDateTime = message.Event.GpsDateTime,
                Altitude = message.Gps.Altitude,
                DeviceId = message.Event.DeviceId,
                IgnitionStatus = message.Payload?.IgnitionStatus ?? false,
                Latitude = message!.Gps!.Location!.Lat,
                Longitude = message!.Gps!.Location!.Lon,
                Odometer = message.Gps.Odometer,
                PowerSignal = message.Payload?.PowerSignal ?? false,
                Satellites = message.Gps.SatellitesFix,
                Speed = message.Gps.Speed,
            };
        }
        public static LatestRecorModel ToLatestRecorModel(this BCEMessage message)
        {
            return new LatestRecorModel(message.Event.DeviceId, message.Event.GpsDateTime,0,0);
        }
        public static Device ToDevice(this BCEMessage model)
        {
            return new Device
            {
                Imei = model.Event.DeviceId,
                Serialno = model.Event.DeviceId.ToString(),
                Phone = model.Event.DeviceId,
                Lastupdated = model.Event.GpsDateTime
            };
        }
        public static Device ToDevice(this SpeedLimiter model)
        {
            return new Device
            {
                Imei = model.DeviceId,
                Serialno = model.DeviceId.ToString(),
                Phone = model.DeviceId,
                Lastupdated = model.GpsDateTime,
                Createdat=DateTime.Now
            };
        }
        public static Position ToPosition(this BCEMessage message)
        {
            return new Position
            {
                Altitude = message.Gps.Altitude,
                Gpsdatetime = message.Event.GpsDateTime,
                Deviceid = message.DeviceId,
                Ignitionstatus = message.Payload?.IgnitionStatus ?? false,
                Latitude = message!.Gps!.Location!.Lat,
                Longitude = message!.Gps!.Location!.Lon,
                Powersignal = message.Payload?.PowerSignal ?? false,
                Satellites = message.Gps.SatellitesFix,
                Speed = message.Gps.Speed,
                Servertime = DateTime.Now
            };
        }
        public static Position ToPosition(this SpeedLimiter message, long deviceId)
        {
            return new Position
            {
                Deviceid = deviceId,
                Altitude = message.Altitude,
                Gpsdatetime = message.GpsDateTime,
                Ignitionstatus = message.IgnitionStatus,
                Latitude = message.Latitude,
                Longitude = message.Longitude,
                Powersignal = message.PowerSignal,
                Satellites = message.Satellites,
                Speed = message.Speed,
                Servertime = DateTime.Now
            };
        }
    }
}


//    1_692_767_801
//1_692_772_582_368
/*
 * The request for sending txt to the server should be in the order 
Send the data using this order. 
date 0
time 1
imei 2
serial 3
vehicleRegistration 4
speed 5
odometer 6
gpsStatus 7 // 0 ok 1 disconnected
numberOfSatellite 8
longitude 9
longitudeDirection 10
latitude 11
latitudeDirection 12
powerSignal 13 // 0 ok 1 disconnected
speedSignal 14 // 0 ok 1 disconnected
ignitionStatus 15 // 0 ok 1 disconnected
alarmReport 16
for example 
2023-05-11,11:14:50,016100005024,,KDG832Y,0.0,0,0,0,34.888168,,0.602883,,0,0,0
ensure you send single records not as a group to create the illusion that the data is being sent from the devices and not the server
 send the data to the IP Address 20.31.49.230 Port 9326 the ntsa will provide a port for you, the data should be sent as plain text with no callback expected

 */
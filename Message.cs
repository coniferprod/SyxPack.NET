#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SyxPack
{
    public enum MessageKind
    {
        UniversalNonRealTime,
        UniversalRealTime,
        ManufacturerSpecific,
    }

    public class UniversalHeader
    {
        public byte DeviceChannel { get; set; }
        public byte SubId1 { get; set; }
        public byte SubId2 { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Device Channel = {0}", this.DeviceChannel + 1));
            builder.AppendLine(string.Format("Sub Id 1 = {0:X2}H, Sub Id 2 = {1:X2}H", this.SubId1, this.SubId2));

            return builder.ToString();
        }
    }

    public abstract class Message
    {
        public List<byte>? Payload;

        protected Message(byte[] data)
        {
            this.Payload = data.ToList();
        }

        /// Creates a message from System Exclusive data bytes.
        public static Message Create(byte[] data)
        {
            byte[] GetPayload(int startIndex = 2)
            {
                return data[startIndex .. ^1];  // leave out the last byte
            }

            UniversalHeader GetUniversalHeader()
            {
                return new UniversalHeader
                {
                    DeviceChannel = data[2],
                    SubId1 = data[3],
                    SubId2 = data[4]
                };
            }

            if (data.Length < 5)
            {
                throw new ArgumentException($"Message too short! ({data.Length} < 5)");
            }

            if (data[0] != Constants.Initiator)
            {
                throw new ArgumentException($"Message must start with {Constants.Initiator:X2}H");
            }

            if (data[^1] != Constants.Terminator)
            {
                throw new ArgumentException($"Message must end with {Constants.Terminator:X2}H");
            }

            switch (data[1])
            {
                case Constants.Development:
                    return new ManufacturerSpecificMessage(
                        GetPayload(),
                        ManufacturerDefinition.Development
                    );

                case Constants.UniversalNonRealTime:
                    return new UniversalMessage(
                        GetPayload(4),
                        GetUniversalHeader()
                    );

                case Constants.UniversalRealTime:
                    return new UniversalMessage(
                        GetPayload(4),
                        GetUniversalHeader(),
                        true
                    );

                case 0x00:  // Extended manufacturer
                    return new ManufacturerSpecificMessage(
                        GetPayload(4),
                        new ManufacturerDefinition(new byte[] { data[1], data[2], data[3] }));

                default:  // Standard manufacturer
                    return new ManufacturerSpecificMessage(
                        GetPayload(),
                        new ManufacturerDefinition(new byte[] { data[1] }));
            }
        }

        public abstract List<byte> ToData();
    }

    public class UniversalMessage : Message
    {
        public bool IsRealtime { get; set; }
        public UniversalHeader Header { get; set; }

        public UniversalMessage(byte[] data, UniversalHeader header, bool realtime = false)
            : base(data)
        {
            this.Header = header;
            this.IsRealtime = realtime;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(
                string.Format("Universal System Exclusive Message, {0}",
                    this.IsRealtime ? "Real-time" : "Non-Real-time"));
            builder.Append(this.Header);

            return builder.ToString();
        }

        public override List<byte> ToData()
        {
            var result = new List<byte>();

            result.Add(Constants.Initiator);

            result.Add(this.IsRealtime ? Constants.UniversalRealTime : Constants.UniversalNonRealTime);
            result.Add(this.Header.DeviceChannel);
            result.Add(this.Header.SubId1);
            result.Add(this.Header.SubId2);

            if (this.Payload != null)
            {
                result.AddRange(this.Payload);
            }

            result.Add(Constants.Terminator);

            return result;
        }
    }

    public class ManufacturerSpecificMessage : Message
    {
        public ManufacturerDefinition Manufacturer { get; }

        public ManufacturerSpecificMessage(byte[] data, ManufacturerDefinition manufacturer)
            : base(data)
        {
            this.Manufacturer = manufacturer;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Manufacturer: {0}", this.Manufacturer));
            builder.AppendLine(string.Format("Payload: {0} bytes", this.Payload?.Count));

            return builder.ToString();
        }

        public override List<byte> ToData()
        {
            var result = new List<byte>();

            result.Add(Constants.Initiator);
            result.AddRange(this.Manufacturer.ToData());

            if (this.Payload != null)
            {
                result.AddRange(this.Payload);
            }

            result.Add(Constants.Terminator);

            return result;
        }
    }
}

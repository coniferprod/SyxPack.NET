using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SyxPack
{
    // Header for Universal System Exclusive message
    public sealed class UniversalMessageHeader : ISystemExclusiveData
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

        //
        // ISystemExclusiveData implementation
        //

        public List<byte> Data
        {
            get
            {
                return new List<byte> { DeviceChannel, SubId1, SubId2 };
            }
        }

        public int DataLength => 3;
    }

    // Abstract base class for System Exclusive messages.
    public abstract class Message : ISystemExclusiveData
    {
        // The payload of the message. This comprises the bytes between
        // the SysEx initiator, manufacturer identifier, and the SysEx terminator.
        public List<byte> Payload;

        // Protected constructor for subclasses.
        protected Message(byte[] payload)
        {
            this.Payload = payload.ToList();
        }

        // Creates a message from System Exclusive data bytes.
        public static Message Create(byte[] data)
        {
            // Local method to extract the payload from the data
            byte[] GetPayload(int startIndex = 2)
            {
                return data[startIndex .. ^1];  // leave out the last byte
            }

            // Local method to extract the Universal message header from the data
            UniversalMessageHeader GetUniversalMessageHeader()
            {
                return new UniversalMessageHeader
                {
                    DeviceChannel = data[2],
                    SubId1 = data[3],
                    SubId2 = data[4]
                };
            }

            // Minimum length is initiator + 1...3 bytes of manufacturer ID + terminator
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
                        ManufacturerDefinition.Development,
                        GetPayload()
                    );

                case Constants.UniversalNonRealTime:
                    return new UniversalMessage(
                        GetUniversalMessageHeader(),
                        GetPayload(4),
                        false
                    );

                case Constants.UniversalRealTime:
                    return new UniversalMessage(
                        GetUniversalMessageHeader(),
                        GetPayload(4),
                        true
                    );

                case 0x00:  // Extended manufacturer
                    return new ManufacturerSpecificMessage(
                        new ManufacturerDefinition(new byte[] { data[1], data[2], data[3] }),
                        GetPayload(4)  // payload starts after SysEx initiator and three-byte identifier
                    );

                default:  // Standard manufacturer
                    return new ManufacturerSpecificMessage(
                        new ManufacturerDefinition(new byte[] { data[1] }),
                        GetPayload()
                    );
            }
        }

        //
        // ISystemExclusiveData implementation
        //

        public abstract List<byte> Data { get; }
        public abstract int DataLength { get; }
    }

    // Represents a Universal System Exclusive message.
    public sealed class UniversalMessage : Message, ISystemExclusiveData
    {
        public UniversalMessageHeader Header { get; set; }
        public bool IsRealtime { get; set; }

        public UniversalMessage(UniversalMessageHeader header, byte[] payload, bool realtime = false)
            : base(payload)
        {
            this.Header = header;
            this.IsRealtime = realtime;
        }

        // Gets a string representation of this message
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(
                string.Format("Universal System Exclusive Message, {0}",
                    this.IsRealtime ? "Real-time" : "Non-Real-time"));
            builder.Append(this.Header);

            return builder.ToString();
        }

        //
        // ISystemExclusiveData implementation
        //

        // Gets the System Exclusive data for this message.
        // This is ready for sending down the wire to a MIDI device,
        // since it includes the SysEx initiator and terminator bytes.
        public override List<byte> Data
        {
            get
            {
                var result = new List<byte>();
                result.Add(Constants.Initiator);
                result.Add(this.IsRealtime ? Constants.UniversalRealTime : Constants.UniversalNonRealTime);
                result.AddRange(this.Header.Data);
                result.AddRange(this.Payload);
                result.Add(Constants.Terminator);
                return result;
            }
        }

        public override int DataLength
        {
            get
            {
                return 2 + this.Header.DataLength + this.Payload.Count + 1;
            }
        }
    }

    // Represents a manufacturer-specific System Exclusive message.
    public class ManufacturerSpecificMessage : Message
    {
        // Read-only property to get the manufacturer.
        public ManufacturerDefinition Manufacturer { get; }

        public ManufacturerSpecificMessage(ManufacturerDefinition manufacturer, byte[] payload)
            : base(payload)
        {
            this.Manufacturer = manufacturer;
        }

        // Gets a string representation of this message.
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Manufacturer: {0}", this.Manufacturer));
            builder.AppendLine(string.Format("Payload: {0} bytes", this.Payload.Count));

            return builder.ToString();
        }

        //
        // ISystemExclusiveData implementation
        //

        // Gets the System Exclusive data for this message.
        // This is ready for sending down the wire to a MIDI device,
        // since it includes the SysEx initiator and terminator bytes.
        public override List<byte> Data
        {
            get
            {
                var result = new List<byte>();
                result.Add(Constants.Initiator);
                result.AddRange(this.Manufacturer.Data);
                result.AddRange(this.Payload);
                result.Add(Constants.Terminator);
                return result;
            }
        }

        public override int DataLength
        {
            get
            {
                return 1 + this.Manufacturer.DataLength + this.Payload.Count + 1;
            }
        }
    }
}

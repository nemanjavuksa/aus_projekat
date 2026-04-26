using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters readParams = (ModbusReadCommandParameters)CommandParameters;

            byte[] request = new byte[12];
            int offset = 0;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.TransactionId)), 0, request, offset, 2);
            offset += 2;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.ProtocolId)), 0, request, offset, 2);
            offset += 2;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Length)), 0, request, offset, 2);
            offset += 2;

            request[offset++] = readParams.UnitId;
            request[offset++] = readParams.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.StartAddress)), 0, request, offset, 2);
            offset += 2;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Quantity)), 0, request, offset, 2);
            offset += 2;

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();
            ModbusReadCommandParameters readParams = (ModbusReadCommandParameters)CommandParameters;

            if ((response[7] & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            int byteCount = response[8];

            for (int i = 0; i < byteCount; i++)
            {
                byte currentByte = response[9 + i];

                for (int bit = 0; bit < 8; bit++)
                {
                    int pointIndex = i * 8 + bit;
                    if (pointIndex >= readParams.Quantity)
                        break;

                    ushort value = (ushort)((currentByte >> bit) & 0x01);

                    result.Add(
                        new Tuple<PointType, ushort>(
                            PointType.DIGITAL_OUTPUT,
                            (ushort)(readParams.StartAddress + pointIndex)),
                        value);
                }
            }

            return result;
        }
    }
}
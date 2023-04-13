// -------------------------------------------------------------------------
// Copyright (c) Mecalc (Pty) Limited. All rights reserved.
// -------------------------------------------------------------------------

using System.Text.Json;

namespace QProtocol.ErrorHandling;

public class QProtocolResponseChecks
{
    /// <summary>
    /// This method can be used to fetch the status code from the HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response as received from QServer.</param>
    /// <returns>The <see cref="StatusCodes"/> returned.</returns>
    public static StatusCodes GetStatusCode(string response)
    {
        return DecodeResponse(response).StatusCode;
    }

    /// <summary>
    /// This method can be used to fetch the stats code from the HTTP response and thow an exception if something went wrong.
    /// </summary>
    /// <param name="response">The HTTP response as received from QServer.</param>
    /// <exception cref="QProtocolException">With the Status code and error message when something went wrong.</exception>
    public static void CheckAndThrow(string? response)
    {
        if (response == null)
        {
            throw new QProtocolException("Invalid response received from REST Request, it may no be null!");
        }

        var info = DecodeResponse(response);
        switch (info.StatusCode)
        {
            case StatusCodes.Updated:
            case StatusCodes.Success:
            case StatusCodes.RequiresRestart:
                break;

            case StatusCodes.Error:
            case StatusCodes.InvalidConfiguration:
            case StatusCodes.InvalidId:
            case StatusCodes.VersionMismatch:
            case StatusCodes.ActionNotFound:
            case StatusCodes.ChannelOnly:
            case StatusCodes.AnalogOutputChannelOnly:
            case StatusCodes.DataChannelOnly:
            case StatusCodes.ChannelDisabled:
            case StatusCodes.ChannelDoesNotSupportTestSignals:
            case StatusCodes.ChannelDoesNotSupportTeds:
            case StatusCodes.ActionHasSideEffects:
            case StatusCodes.AutoZeroNotSupported:
            case StatusCodes.AutoZeroFailed:
            case StatusCodes.ReadingStatusRegisterFailed:
            case StatusCodes.StatusRegisterNotSupported:
            case StatusCodes.CanFdChannelOnly:
            default:
                throw new QProtocolException(info.StatusCode, info.Message);
        }
    }

    private static QProtocolResponse DecodeResponse(string response)
    {
        if (response.Contains("TypeCode") && response.Contains("StatusCode") && response.Contains("Message"))
        {
            try
            {
                var qServerResponse = JsonSerializer.Deserialize<QProtocolResponse>(response);
                if (qServerResponse != null)
                {
                    return qServerResponse;
                }
            }
            catch (Exception)
            {
                throw new ApplicationException($"Unable to decode the response given from QServer: {response}");
            }
        }

        return new QProtocolResponse() { StatusCode = StatusCodes.Success };
    }
}

using System;
using System.Runtime.InteropServices;

namespace UdtSharp
{
    public class UdtException : Exception
    {
        int m_iMajor;
        int m_iMinor;
        int m_iErrno;

        public UdtException(int major = 0, int minor = 0, int err = -1)
            : base(getErrorMessage(major, minor))
        {
            m_iMajor = major;
            m_iMinor = minor;
            if (-1 == err)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    m_iErrno = Marshal.GetLastWin32Error();

                // TODO handle non-windows error
            }
            else
                m_iErrno = err;
        }

        private static string getErrorMessage(int major, int minor)
        {
            // translate "Major:Minor" code into text message.

            string strMsg = string.Empty;

            switch (major)
            {
                case 0:
                    strMsg = "Success";
                    break;

                case 1:
                    strMsg = "Connection setup failure";

                    switch (minor)
                    {
                        case 1:
                            strMsg += ": connection time out";
                            break;

                        case 2:
                            strMsg += ": connection rejected";
                            break;

                        case 3:
                            strMsg += ": unable to create/configure UDP socket";
                            break;

                        case 4:
                            strMsg += ": abort for security reasons";
                            break;

                        default:
                            break;
                    }

                    break;

                case 2:
                    switch (minor)
                    {
                        case 1:
                            strMsg = "Connection was broken";
                            break;

                        case 2:
                            strMsg = "Connection does not exist";
                            break;

                        default:
                            break;
                    }

                    break;

                case 3:
                    strMsg = "System resource failure";

                    switch (minor)
                    {
                        case 1:
                            strMsg += ": unable to create new threads";
                            break;

                        case 2:
                            strMsg += ": unable to allocate buffers";
                            break;

                        default:
                            break;
                    }

                    break;

                case 4:
                    strMsg = "File system failure";

                    switch (minor)
                    {
                        case 1:
                            strMsg += ": cannot seek read position";
                            break;

                        case 2:
                            strMsg += ": failure in read";
                            break;

                        case 3:
                            strMsg += ": cannot seek write position";
                            break;

                        case 4:
                            strMsg += ": failure in write";
                            break;

                        default:
                            break;
                    }

                    break;

                case 5:
                    strMsg = "Operation not supported";

                    switch (minor)
                    {
                        case 1:
                            strMsg += ": Cannot do this operation on a BOUND socket";
                            break;

                        case 2:
                            strMsg += ": Cannot do this operation on a CONNECTED socket";
                            break;

                        case 3:
                            strMsg += ": Bad parameters";
                            break;

                        case 4:
                            strMsg += ": Invalid socket ID";
                            break;

                        case 5:
                            strMsg += ": Cannot do this operation on an UNBOUND socket";
                            break;

                        case 6:
                            strMsg += ": Socket is not in listening state";
                            break;

                        case 7:
                            strMsg += ": Listen/accept is not supported in rendezous connection setup";
                            break;

                        case 8:
                            strMsg += ": Cannot call connect on UNBOUND socket in rendezvous connection setup";
                            break;

                        case 9:
                            strMsg += ": This operation is not supported in SOCK_STREAM mode";
                            break;

                        case 10:
                            strMsg += ": This operation is not supported in SOCK_DGRAM mode";
                            break;

                        case 11:
                            strMsg += ": Another socket is already listening on the same port";
                            break;

                        case 12:
                            strMsg += ": Message is too large to send (it must be less than the UDT send buffer size)";
                            break;

                        case 13:
                            strMsg += ": Invalid epoll ID";
                            break;

                        default:
                            break;
                    }

                    break;

                case 6:
                    strMsg = "Non-blocking call failure";

                    switch (minor)
                    {
                        case 1:
                            strMsg += ": no buffer available for sending";
                            break;

                        case 2:
                            strMsg += ": no data available for reading";
                            break;

                        default:
                            break;
                    }

                    break;

                case 7:
                    strMsg = "The peer side has signalled an error";

                    break;

                default:
                    strMsg = "Unknown error";
                    break;
            }

            //        // Adding "errno" information
            //        if ((0 != m_iMajor) && (0 < m_iErrno))
            //        {
            //            strMsg += ": ";
            //# ifndef WIN32
            //            char errmsg[1024];
            //            if (strerror_r(m_iErrno, errmsg, 1024) == 0)
            //                strMsg += errmsg;
            //#else
            //            LPVOID lpMsgBuf;
            //            FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, m_iErrno, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) & lpMsgBuf, 0, NULL);
            //            strMsg += (char*)lpMsgBuf;
            //            LocalFree(lpMsgBuf);
            //#endif
            //        }

            return strMsg;
        }

        public int getErrorCode()
        {
            return m_iMajor * 1000 + m_iMinor;
        }

        void clear()
        {
            m_iMajor = 0;
            m_iMinor = 0;
            m_iErrno = 0;
        }

        const int SUCCESS = 0;
        const int ECONNSETUP = 1000;
        const int ENOSERVER = 1001;
        const int ECONNREJ = 1002;
        const int ESOCKFAIL = 1003;
        const int ESECFAIL = 1004;
        const int ECONNFAIL = 2000;
        const int ECONNLOST = 2001;
        const int ENOCONN = 2002;
        const int ERESOURCE = 3000;
        const int ETHREAD = 3001;
        const int ENOBUF = 3002;
        const int EFILE = 4000;
        const int EINVRDOFF = 4001;
        const int ERDPERM = 4002;
        const int EINVWROFF = 4003;
        const int EWRPERM = 4004;
        const int EINVOP = 5000;
        const int EBOUNDSOCK = 5001;
        const int ECONNSOCK = 5002;
        const int EINVPARAM = 5003;
        const int EINVSOCK = 5004;
        const int EUNBOUNDSOCK = 5005;
        const int ENOLISTEN = 5006;
        const int ERDVNOSERV = 5007;
        const int ERDVUNBOUND = 5008;
        const int ESTREAMILL = 5009;
        const int EDGRAMILL = 5010;
        const int EDUPLISTEN = 5011;
        const int ELARGEMSG = 5012;
        const int EINVPOLLID = 5013;
        const int EASYNCFAIL = 6000;
        const int EASYNCSND = 6001;
        const int EASYNCRCV = 6002;
        const int ETIMEOUT = 6003;
        const int EPEERERR = 7000;
        const int EUNKNOWN = -1;

    }
}

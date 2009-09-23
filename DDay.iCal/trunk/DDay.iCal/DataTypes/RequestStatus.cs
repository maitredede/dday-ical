using System;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace DDay.iCal.DataTypes
{
    /// <summary>
    /// A class that represents the return status of an iCalendar request.
    /// </summary>
    [DebuggerDisplay("{StatusCode} - {StatusDesc}")]
    public class RequestStatus : iCalDataType
    {
        #region Private Fields

        private Text m_StatusDesc;
        private Text m_ExtData;
        private StatusCode m_StatusCode;
        
        #endregion

        #region Public Properties

        public Text StatusDesc
        {
            get { return m_StatusDesc; }
            set { m_StatusDesc = value; }
        }

        public Text ExtData
        {
            get { return m_ExtData; }
            set { m_ExtData = value; }
        }

        public StatusCode StatusCode
        {
            get { return m_StatusCode; }
            set { m_StatusCode = value; }
        }

        #endregion

        #region Constructors

        public RequestStatus() { }
        public RequestStatus(string value)
            : this()
        {
            CopyFrom(Parse(value));
        }

        #endregion

        #region Overrides

        public override void CopyFrom(object obj)
        {
            base.CopyFrom(obj);
            if (obj is RequestStatus)
            {
                RequestStatus rs = (RequestStatus)obj;
                if (rs.StatusCode != null)
                    StatusCode = (StatusCode)rs.StatusCode.Copy();
                if (rs.StatusDesc != null)
                    StatusDesc = (Text)rs.StatusDesc.Copy();
                if (rs.ExtData != null)
                    ExtData = (Text)rs.ExtData.Copy();
            }
            base.CopyFrom(obj);
        }

        public override bool TryParse(string value, ref object obj)
        {
            RequestStatus rs = (RequestStatus)obj;
            Match match = Regex.Match(value, @"(.+);(.+)(;(.*))?");
            if (match.Success)
            {
                if (!match.Groups[1].Success || 
                    !match.Groups[2].Success)
                    return false;

                StatusCode = new StatusCode(match.Groups[1].Value);
                StatusDesc = new Text(match.Groups[1].Value, true);
                if (match.Groups[3].Success)
                    ExtData = new Text(match.Groups[4].Value, true);

                return true;
            }
            return false;            
        }

        #endregion
    }
}

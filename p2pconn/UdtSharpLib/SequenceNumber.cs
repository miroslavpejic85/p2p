// UDT Sequence Number 0 - (2^31 - 1)

// seqcmp: compare two seq#, considering the wraping
// seqlen: length from the 1st to the 2nd seq#, including both
// seqoff: offset from the 2nd to the 1st seq#
// incseq: increase the seq# by 1
// decseq: decrease the seq# by 1
// incseq: increase the seq# by a given offset

using System;

namespace UdtSharp
{
    static class SequenceNumber
    {
        public static int seqcmp(int seq1, int seq2)
        { 
            return (Math.Abs(seq1 - seq2) < m_iSeqNoTH) ? (seq1 - seq2) : (seq2 - seq1);
        }

        public static int seqlen(int seq1, int seq2)
        {
            return (seq1 <= seq2) ? (seq2 - seq1 + 1) : (seq2 - seq1 + m_iMaxSeqNo + 2);
        }

        public static int seqoff(int seq1, int seq2)
        {
            if (Math.Abs(seq1 - seq2) < m_iSeqNoTH)
                return seq2 - seq1;

            if (seq1 < seq2)
                return seq2 - seq1 - m_iMaxSeqNo - 1;

            return seq2 - seq1 + m_iMaxSeqNo + 1;
        }

        public static int incseq(int seq)
        {
            return (seq == m_iMaxSeqNo) ? 0 : seq + 1;
        }

        public static int decseq(int seq)
        {
            return (seq == 0) ? m_iMaxSeqNo : seq - 1;
        }

        public static int incseq(int seq, int inc)
        {
            return (m_iMaxSeqNo - seq >= inc) ? seq + inc : seq - m_iMaxSeqNo + inc - 1;
        }

        public static int m_iSeqNoTH = 0x3FFFFFFF;             // threshold for comparing seq. no.
        public static int m_iMaxSeqNo = 0x7FFFFFFF;            // maximum sequence number used in UDT
    }
}
namespace UdtSharp
{
    public class SndLossList
    {
        int[] m_piData1;                  // sequence number starts
        int[] m_piData2;                  // seqnence number ends
        int[] m_piNext;                       // next node in the list

        int m_iHead;                         // first node
        int m_iLength;                       // loss length
        int m_iSize;                         // size of the static array
        int m_iLastInsertPos;                // position of last insert node

        object m_ListLock = new object();          // used to synchronize list operation

        public SndLossList(int size)
        {
            m_iHead = -1;
            m_iLength = 0;
            m_iSize = size;
            m_iLastInsertPos = -1;

            m_piData1 = new int[m_iSize];
            m_piData2 = new int[m_iSize];
            m_piNext = new int[m_iSize];

            // -1 means there is no data in the node
            for (int i = 0; i < size; ++i)
            {
                m_piData1[i] = -1;
                m_piData2[i] = -1;
            }

        }

        public int insert(int seqno1, int seqno2)
        {
            lock (m_ListLock)
            {
                return insert_unsafe(seqno1, seqno2);
            }
        }

        int insert_unsafe(int seqno1, int seqno2)
        {
            if (0 == m_iLength)
            {
                // insert data into an empty list

                m_iHead = 0;
                m_piData1[m_iHead] = seqno1;
                if (seqno2 != seqno1)
                    m_piData2[m_iHead] = seqno2;

                m_piNext[m_iHead] = -1;
                m_iLastInsertPos = m_iHead;

                m_iLength += SequenceNumber.seqlen(seqno1, seqno2);

                return m_iLength;
            }

            // otherwise find the position where the data can be inserted
            int origlen = m_iLength;
            int offset = SequenceNumber.seqoff(m_piData1[m_iHead], seqno1);
            int loc = (m_iHead + offset + m_iSize) % m_iSize;

            if (offset < 0)
            {
                // Insert data prior to the head pointer

                m_piData1[loc] = seqno1;
                if (seqno2 != seqno1)
                    m_piData2[loc] = seqno2;

                // new node becomes head
                m_piNext[loc] = m_iHead;
                m_iHead = loc;
                m_iLastInsertPos = loc;

                m_iLength += SequenceNumber.seqlen(seqno1, seqno2);
            }
            else if (offset > 0)
            {
                if (seqno1 == m_piData1[loc])
                {
                    m_iLastInsertPos = loc;

                    // first seqno is equivlent, compare the second
                    if (-1 == m_piData2[loc])
                    {
                        if (seqno2 != seqno1)
                        {
                            m_iLength += SequenceNumber.seqlen(seqno1, seqno2) - 1;
                            m_piData2[loc] = seqno2;
                        }
                    }
                    else if (SequenceNumber.seqcmp(seqno2, m_piData2[loc]) > 0)
                    {
                        // new seq pair is longer than old pair, e.g., insert [3, 7] to [3, 5], becomes [3, 7]
                        m_iLength += SequenceNumber.seqlen(m_piData2[loc], seqno2) - 1;
                        m_piData2[loc] = seqno2;
                    }
                    else
                        // Do nothing if it is already there
                        return 0;
                }
                else
                {
                    // searching the prior node
                    int i;
                    if ((-1 != m_iLastInsertPos) && (SequenceNumber.seqcmp(m_piData1[m_iLastInsertPos], seqno1) < 0))
                        i = m_iLastInsertPos;
                    else
                        i = m_iHead;

                    while ((-1 != m_piNext[i]) && (SequenceNumber.seqcmp(m_piData1[m_piNext[i]], seqno1) < 0))
                        i = m_piNext[i];

                    if ((-1 == m_piData2[i]) || (SequenceNumber.seqcmp(m_piData2[i], seqno1) < 0))
                    {
                        m_iLastInsertPos = loc;

                        // no overlap, create new node
                        m_piData1[loc] = seqno1;
                        if (seqno2 != seqno1)
                            m_piData2[loc] = seqno2;

                        m_piNext[loc] = m_piNext[i];
                        m_piNext[i] = loc;

                        m_iLength += SequenceNumber.seqlen(seqno1, seqno2);
                    }
                    else
                    {
                        m_iLastInsertPos = i;

                        // overlap, coalesce with prior node, insert(3, 7) to [2, 5], ... becomes [2, 7]
                        if (SequenceNumber.seqcmp(m_piData2[i], seqno2) < 0)
                        {
                            m_iLength += SequenceNumber.seqlen(m_piData2[i], seqno2) - 1;
                            m_piData2[i] = seqno2;

                            loc = i;
                        }
                        else
                            return 0;
                    }
                }
            }
            else
            {
                m_iLastInsertPos = m_iHead;

                // insert to head node
                if (seqno2 != seqno1)
                {
                    if (-1 == m_piData2[loc])
                    {
                        m_iLength += SequenceNumber.seqlen(seqno1, seqno2) - 1;
                        m_piData2[loc] = seqno2;
                    }
                    else if (SequenceNumber.seqcmp(seqno2, m_piData2[loc]) > 0)
                    {
                        m_iLength += SequenceNumber.seqlen(m_piData2[loc], seqno2) - 1;
                        m_piData2[loc] = seqno2;
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }

            // coalesce with next node. E.g., [3, 7], ..., [6, 9] becomes [3, 9] 
            while ((-1 != m_piNext[loc]) && (-1 != m_piData2[loc]))
            {
                int i = m_piNext[loc];

                if (SequenceNumber.seqcmp(m_piData1[i], SequenceNumber.incseq(m_piData2[loc])) <= 0)
                {
                    // coalesce if there is overlap
                    if (-1 != m_piData2[i])
                    {
                        if (SequenceNumber.seqcmp(m_piData2[i], m_piData2[loc]) > 0)
                        {
                            if (SequenceNumber.seqcmp(m_piData2[loc], m_piData1[i]) >= 0)
                                m_iLength -= SequenceNumber.seqlen(m_piData1[i], m_piData2[loc]);

                            m_piData2[loc] = m_piData2[i];
                        }
                        else
                            m_iLength -= SequenceNumber.seqlen(m_piData1[i], m_piData2[i]);
                    }
                    else
                    {
                        if (m_piData1[i] == SequenceNumber.incseq(m_piData2[loc]))
                            m_piData2[loc] = m_piData1[i];
                        else
                            m_iLength--;
                    }

                    m_piData1[i] = -1;
                    m_piData2[i] = -1;
                    m_piNext[loc] = m_piNext[i];
                }
                else
                    break;
            }

            return m_iLength - origlen;
        }

        public void remove(int seqno)
        {
            lock (m_ListLock)
            {
                remove_unsafe(seqno);
            }
        }

        void remove_unsafe(int seqno)
        {
            if (0 == m_iLength)
                return;

            // Remove all from the head pointer to a node with a larger seq. no. or the list is empty
            int offset = SequenceNumber.seqoff(m_piData1[m_iHead], seqno);
            int loc = (m_iHead + offset + m_iSize) % m_iSize;

            if (0 == offset)
            {
                // It is the head. Remove the head and point to the next node
                loc = (loc + 1) % m_iSize;

                if (-1 == m_piData2[m_iHead])
                    loc = m_piNext[m_iHead];
                else
                {
                    m_piData1[loc] = SequenceNumber.incseq(seqno);
                    if (SequenceNumber.seqcmp(m_piData2[m_iHead], SequenceNumber.incseq(seqno)) > 0)
                        m_piData2[loc] = m_piData2[m_iHead];

                    m_piData2[m_iHead] = -1;

                    m_piNext[loc] = m_piNext[m_iHead];
                }

                m_piData1[m_iHead] = -1;

                if (m_iLastInsertPos == m_iHead)
                    m_iLastInsertPos = -1;

                m_iHead = loc;

                m_iLength--;
            }
            else if (offset > 0)
            {
                int h = m_iHead;

                if (seqno == m_piData1[loc])
                {
                    // target node is not empty, remove part/all of the seqno in the node.
                    int temp = loc;
                    loc = (loc + 1) % m_iSize;

                    if (-1 == m_piData2[temp])
                        m_iHead = m_piNext[temp];
                    else
                    {
                        // remove part, e.g., [3, 7] becomes [], [4, 7] after remove(3)
                        m_piData1[loc] = SequenceNumber.incseq(seqno);
                        if (SequenceNumber.seqcmp(m_piData2[temp], m_piData1[loc]) > 0)
                            m_piData2[loc] = m_piData2[temp];
                        m_iHead = loc;
                        m_piNext[loc] = m_piNext[temp];
                        m_piNext[temp] = loc;
                        m_piData2[temp] = -1;
                    }
                }
                else
                {
                    // target node is empty, check prior node
                    int i = m_iHead;
                    while ((-1 != m_piNext[i]) && (SequenceNumber.seqcmp(m_piData1[m_piNext[i]], seqno) < 0))
                        i = m_piNext[i];

                    loc = (loc + 1) % m_iSize;

                    if (-1 == m_piData2[i])
                        m_iHead = m_piNext[i];
                    else if (SequenceNumber.seqcmp(m_piData2[i], seqno) > 0)
                    {
                        // remove part/all seqno in the prior node
                        m_piData1[loc] = SequenceNumber.incseq(seqno);
                        if (SequenceNumber.seqcmp(m_piData2[i], m_piData1[loc]) > 0)
                            m_piData2[loc] = m_piData2[i];

                        m_piData2[i] = seqno;

                        m_piNext[loc] = m_piNext[i];
                        m_piNext[i] = loc;

                        m_iHead = loc;
                    }
                    else
                        m_iHead = m_piNext[i];
                }

                // Remove all nodes prior to the new head
                while (h != m_iHead)
                {
                    if (m_piData2[h] != -1)
                    {
                        m_iLength -= SequenceNumber.seqlen(m_piData1[h], m_piData2[h]);
                        m_piData2[h] = -1;
                    }
                    else
                        m_iLength--;

                    m_piData1[h] = -1;

                    if (m_iLastInsertPos == h)
                        m_iLastInsertPos = -1;

                    h = m_piNext[h];
                }
            }
        }

        public int getLossLength()
        {
            lock (m_ListLock)
            {
                return m_iLength;
            }
        }

        public int getLostSeq()
        {
            if (0 == m_iLength)
                return -1;

            lock (m_ListLock)
            {

                if (0 == m_iLength)
                    return -1;

                if (m_iLastInsertPos == m_iHead)
                    m_iLastInsertPos = -1;

                // return the first loss seq. no.
                int seqno = m_piData1[m_iHead];

                // head moves to the next node
                if (-1 == m_piData2[m_iHead])
                {
                    //[3, -1] becomes [], and head moves to next node in the list
                    m_piData1[m_iHead] = -1;
                    m_iHead = m_piNext[m_iHead];
                }
                else
                {
                    // shift to next node, e.g., [3, 7] becomes [], [4, 7]
                    int loc = (m_iHead + 1) % m_iSize;

                    m_piData1[loc] = SequenceNumber.incseq(seqno);
                    if (SequenceNumber.seqcmp(m_piData2[m_iHead], m_piData1[loc]) > 0)
                        m_piData2[loc] = m_piData2[m_iHead];

                    m_piData1[m_iHead] = -1;
                    m_piData2[m_iHead] = -1;

                    m_piNext[loc] = m_piNext[m_iHead];
                    m_iHead = loc;
                }

                m_iLength--;

                return seqno;
            }
        }
    }

    public class RcvLossList
    {

        int[] m_piData1;                  // sequence number starts
        int[] m_piData2;                  // sequence number ends
        int[] m_piNext;                       // next node in the list
        int[] m_piPrior;                      // prior node in the list;

        int m_iHead;                         // first node in the list
        int m_iTail;                         // last node in the list;
        int m_iLength;                       // loss length
        int m_iSize;                         // size of the static array

        public RcvLossList(int size)
        {
            m_iHead = -1;
            m_iTail = -1;
            m_iLength = 0;
            m_iSize = size;
            m_piData1 = new int[m_iSize];
            m_piData2 = new int[m_iSize];
            m_piNext = new int[m_iSize];
            m_piPrior = new int[m_iSize];

            // -1 means there is no data in the node
            for (int i = 0; i < size; ++i)
            {
                m_piData1[i] = -1;
                m_piData2[i] = -1;
            }
        }


        public void insert(int seqno1, int seqno2)
        {
            // Data to be inserted must be larger than all those in the list
            // guaranteed by the UDT receiver

            if (0 == m_iLength)
            {
                // insert data into an empty list
                m_iHead = 0;
                m_iTail = 0;
                m_piData1[m_iHead] = seqno1;
                if (seqno2 != seqno1)
                    m_piData2[m_iHead] = seqno2;

                m_piNext[m_iHead] = -1;
                m_piPrior[m_iHead] = -1;
                m_iLength += SequenceNumber.seqlen(seqno1, seqno2);

                return;
            }

            // otherwise searching for the position where the node should be
            int offset = SequenceNumber.seqoff(m_piData1[m_iHead], seqno1);
            int loc = (m_iHead + offset) % m_iSize;

            if ((-1 != m_piData2[m_iTail]) && (SequenceNumber.incseq(m_piData2[m_iTail]) == seqno1))
            {
                // coalesce with prior node, e.g., [2, 5], [6, 7] becomes [2, 7]
                loc = m_iTail;
                m_piData2[loc] = seqno2;
            }
            else
            {
                // create new node
                m_piData1[loc] = seqno1;

                if (seqno2 != seqno1)
                    m_piData2[loc] = seqno2;

                m_piNext[m_iTail] = loc;
                m_piPrior[loc] = m_iTail;
                m_piNext[loc] = -1;
                m_iTail = loc;
            }

            m_iLength += SequenceNumber.seqlen(seqno1, seqno2);
        }

        public bool remove(int seqno)
        {
            if (0 == m_iLength)
                return false;

            // locate the position of "seqno" in the list
            int offset = SequenceNumber.seqoff(m_piData1[m_iHead], seqno);
            if (offset < 0)
                return false;

            int loc = (m_iHead + offset) % m_iSize;

            if (seqno == m_piData1[loc])
            {
                // This is a seq. no. that starts the loss sequence

                if (-1 == m_piData2[loc])
                {
                    // there is only 1 loss in the sequence, delete it from the node
                    if (m_iHead == loc)
                    {
                        m_iHead = m_piNext[m_iHead];
                        if (-1 != m_iHead)
                            m_piPrior[m_iHead] = -1;
                    }
                    else
                    {
                        m_piNext[m_piPrior[loc]] = m_piNext[loc];
                        if (-1 != m_piNext[loc])
                            m_piPrior[m_piNext[loc]] = m_piPrior[loc];
                        else
                            m_iTail = m_piPrior[loc];
                    }

                    m_piData1[loc] = -1;
                }
                else
                {
                    // there are more than 1 loss in the sequence
                    // move the node to the next and update the starter as the next loss inSeqNo(seqno)

                    // find next node
                    int j = (loc + 1) % m_iSize;

                    // remove the "seqno" and change the starter as next seq. no.
                    m_piData1[j] = SequenceNumber.incseq(m_piData1[loc]);

                    // process the sequence end
                    if (SequenceNumber.seqcmp(m_piData2[loc], SequenceNumber.incseq(m_piData1[loc])) > 0)
                        m_piData2[j] = m_piData2[loc];

                    // remove the current node
                    m_piData1[loc] = -1;
                    m_piData2[loc] = -1;

                    // update list pointer
                    m_piNext[j] = m_piNext[loc];
                    m_piPrior[j] = m_piPrior[loc];

                    if (m_iHead == loc)
                        m_iHead = j;
                    else
                        m_piNext[m_piPrior[j]] = j;

                    if (m_iTail == loc)
                        m_iTail = j;
                    else
                        m_piPrior[m_piNext[j]] = j;
                }

                m_iLength--;

                return true;
            }

            // There is no loss sequence in the current position
            // the "seqno" may be contained in a previous node

            // searching previous node
            int i = (loc - 1 + m_iSize) % m_iSize;
            while (-1 == m_piData1[i])
                i = (i - 1 + m_iSize) % m_iSize;

            // not contained in this node, return
            if ((-1 == m_piData2[i]) || (SequenceNumber.seqcmp(seqno, m_piData2[i]) > 0))
                return false;

            if (seqno == m_piData2[i])
            {
                // it is the sequence end

                if (seqno == SequenceNumber.incseq(m_piData1[i]))
                    m_piData2[i] = -1;
                else
                    m_piData2[i] = SequenceNumber.decseq(seqno);
            }
            else
            {
                // split the sequence

                // construct the second sequence from SequenceNumber.incseq(seqno) to the original sequence end
                // located at "loc + 1"
                loc = (loc + 1) % m_iSize;

                m_piData1[loc] = SequenceNumber.incseq(seqno);
                if (SequenceNumber.seqcmp(m_piData2[i], m_piData1[loc]) > 0)
                    m_piData2[loc] = m_piData2[i];

                // the first (original) sequence is between the original sequence start to SequenceNumber.decseq(seqno)
                if (seqno == SequenceNumber.incseq(m_piData1[i]))
                    m_piData2[i] = -1;
                else
                    m_piData2[i] = SequenceNumber.decseq(seqno);

                // update the list pointer
                m_piNext[loc] = m_piNext[i];
                m_piNext[i] = loc;
                m_piPrior[loc] = i;

                if (m_iTail == i)
                    m_iTail = loc;
                else
                    m_piPrior[m_piNext[loc]] = loc;
            }

            m_iLength--;

            return true;
        }

        public bool remove(int seqno1, int seqno2)
        {
            if (seqno1 <= seqno2)
            {
                for (int i = seqno1; i <= seqno2; ++i)
                    remove(i);
            }
            else
            {
                for (int j = seqno1; j < SequenceNumber.m_iMaxSeqNo; ++j)
                    remove(j);
                for (int k = 0; k <= seqno2; ++k)
                    remove(k);
            }

            return true;
        }

        bool find(int seqno1, int seqno2)
        {
            if (0 == m_iLength)
                return false;

            int p = m_iHead;

            while (-1 != p)
            {
                if ((SequenceNumber.seqcmp(m_piData1[p], seqno1) == 0) ||
                    ((SequenceNumber.seqcmp(m_piData1[p], seqno1) > 0) && (SequenceNumber.seqcmp(m_piData1[p], seqno2) <= 0)) ||
                    ((SequenceNumber.seqcmp(m_piData1[p], seqno1) < 0) && (m_piData2[p] != -1) && SequenceNumber.seqcmp(m_piData2[p], seqno1) >= 0))
                    return true;

                p = m_piNext[p];
            }

            return false;
        }

        public int getLossLength()
        {
            return m_iLength;
        }

        public int getFirstLostSeq()
        {
            if (0 == m_iLength)
                return -1;

            return m_piData1[m_iHead];
        }

        public void getLossArray(int[] array, out int len, int limit)
        {
            len = 0;

            int i = m_iHead;

            while ((len < limit - 1) && (-1 != i))
            {
                array[len] = m_piData1[i];
                if (-1 != m_piData2[i])
                {
                    // there are more than 1 loss in the sequence
                    array[len] = (int)((uint)array[len] | 0x80000000);
                    ++len;
                    array[len] = m_piData2[i];
                }

                ++len;

                i = m_piNext[i];
            }
        }
    }
}
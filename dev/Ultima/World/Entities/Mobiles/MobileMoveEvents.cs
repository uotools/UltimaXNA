﻿using System;

namespace UltimaXNA.Ultima.World.Entities.Mobiles
{
    /// <summary>
    /// Queues moves and maintains the fastwalk key and current sequence value.
    /// </summary>
    class MobileMoveEvents
    {
        private int m_LastSequenceAck;
        private int m_SequenceQueued;
        private int m_SequenceNextSend;
        private int m_FastWalkKey;
        MobileMoveEvent[] m_History;

        public bool SlowSync
        {
            get
            {
                if (m_SequenceNextSend > m_LastSequenceAck + 4)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public MobileMoveEvents()
        {
            ResetMoveSequence();
        }

        public void ResetMoveSequence()
        {
            m_SequenceQueued = 0;
            m_LastSequenceAck = -1;
            m_SequenceNextSend = 0;
            m_FastWalkKey = new Random().Next(int.MinValue, int.MaxValue);
            m_History = new MobileMoveEvent[256];
        }

        public void AddMoveEvent(int x, int y, int z, int facing, bool createdByPlayerInput)
        {
            MobileMoveEvent moveEvent = new MobileMoveEvent(x, y, z, facing, m_FastWalkKey);
            moveEvent.CreatedByPlayerInput = createdByPlayerInput;

            m_History[m_SequenceQueued] = moveEvent;

            m_SequenceQueued += 1;
            if (m_SequenceQueued > byte.MaxValue)
                m_SequenceQueued = 1;
        }

        public MobileMoveEvent GetNextMoveEvent(out int sequence)
        {
            if (m_History[m_SequenceNextSend] != null)
            {
                MobileMoveEvent m = m_History[m_SequenceNextSend];
                m_History[m_SequenceNextSend] = null;
                sequence = m_SequenceNextSend;
                m_SequenceNextSend++;
                if (m_SequenceNextSend > byte.MaxValue)
                    m_SequenceNextSend = 1;
                return m;
            }
            else
            {
                sequence = 0;
                return null;
            }
        }

        public MobileMoveEvent PeekNextMoveEvent()
        {
            if (m_History[m_SequenceNextSend] != null)
            {
                MobileMoveEvent m = m_History[m_SequenceNextSend];
                return m;
            }
            else
            {
                return null;
            }
        }

        public MobileMoveEvent GetFinalMoveEvent(out int sequence)
        {
            MobileMoveEvent moveEvent = null;
            MobileMoveEvent moveEventNext, moveEventLast;

            while ((moveEventNext = GetNextMoveEvent(out sequence)) != null)
            {
                // save the last moveEvent.
                moveEventLast = moveEvent;
                // get the next move event, erasing it from the queued move events.
                moveEvent = moveEventNext;
                // get the next move event, peeking to see if it is null (this does not erase it from the queue).
                moveEventNext = PeekNextMoveEvent();
                // we want to save move events that are the last move event in the queue, and are only facing changes.
                if ((moveEventNext == null) && (moveEventLast != null) &&
                    (moveEvent.X == moveEventLast.X) && (moveEvent.Y == moveEventLast.Y) && (moveEventLast.Z == moveEvent.Z) &&
                    (moveEvent.Facing != moveEventLast.Facing))
                {
                    // re-queue the final facing change, and return the second-to-last move event.
                    AddMoveEvent(moveEvent.X, moveEvent.Y, moveEvent.Z, moveEvent.Facing, false);
                    return moveEventLast;
                }
            }
            return moveEvent;
        }

        public void AcknowledgemMoveRequest(int sequence)
        {
            m_History[sequence] = null;
            m_LastSequenceAck = sequence;
        }

        public void RejectMoveRequest(int sequence, out int x, out int y, out int z, out int facing)
        {
            if (m_History[sequence] != null)
            {
                MobileMoveEvent e = m_History[sequence];
                x = e.X;
                y = e.Y;
                z = e.Z;
                facing = e.Facing;
            }
            else
            {
                x = y = z = facing = -1;
            }
            ResetMoveSequence();
        }
    }

    class MobileMoveEvent
    {
        public bool CreatedByPlayerInput = false;
        public readonly int X, Y, Z, Facing, Fastwalk;

        public MobileMoveEvent(int x, int y, int z, int facing, int fastwalk)
        {
            X = x;
            Y = y;
            Z = z;
            Facing = facing;
            Fastwalk = fastwalk;
        }
    }
}
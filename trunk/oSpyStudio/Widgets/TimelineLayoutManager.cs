//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;

namespace oSpyStudio.Widgets
{
    public class TimelineLayoutManager
    {
        protected readonly uint m_xmargin = 5;
        protected readonly uint m_ymargin = 10;
        protected readonly uint m_xpadding = 3;
        protected readonly uint m_ypadding = 6;

        protected ITimelineModel m_model;
        protected uint m_rowCount;

        public uint XMargin
        {
            get
            {
                return m_xmargin;
            }
        }

        public uint YMargin
        {
            get
            {
                return m_ymargin;
            }
        }

        public uint XPadding
        {
            get
            {
                return m_xpadding;
            }
        }

        public uint YPadding
        {
            get
            {
                return m_ypadding;
            }
        }

        public ITimelineModel Model
        {
            get
            {
                return m_model;
            }
        }

        public uint RowCount
        {
            get
            {
                return m_rowCount;
            }
        }

        public TimelineLayoutManager(ITimelineModel model)
        {
            m_model = model;
        }

        public void Update()
        {
            Road road = new Road(m_xmargin, m_ymargin, m_xpadding, m_ypadding);
            foreach (ITimelineNode node in m_model.Nodes)
            {
                Lane lane = road.FindExistingLaneFor(node);
                if (lane == null)
                {
                    ITimelineNode lastNodeInLane = FindLastNodeWithContext(node.Context);
                    lane = road.ReserveLaneUntil(lastNodeInLane);
                }

                lane.Append(node);
            }

            road.UpdateLaneOffsets();

            foreach (Lane lane in road.Lanes)
            {
                foreach (Car car in lane.Cars)
                {
                    ITimelineNode node = car.Driver;

                    node.Position = new Point(car.Offset, lane.StartOffset);
                }
            }

            m_rowCount = (uint) road.Lanes.Count;
        }

        private ITimelineNode FindLastNodeWithContext(object context)
        {
            foreach (ITimelineNode node in m_model.NodesReverse)
            {
                if (node.Context == context)
                    return node;
            }

            throw new NotSupportedException("Should not get here");
        }

        internal class Road
        {
            private readonly uint m_startMargin;
            private readonly uint m_shoulderMargin;
            private readonly uint m_markerDistance;
            private readonly uint m_laneDistance;
            private uint m_curOffset;

            private List<Marker> m_markers = new List<Marker>();
            private List<Lane> m_lanes = new List<Lane>();

            public List<Lane> Lanes
            {
                get
                {
                    return m_lanes;
                }
            }

            public Road(uint startMargin, uint shoulderMargin, uint markerDistance, uint laneDistance)
            {
                m_startMargin = startMargin;
                m_shoulderMargin = shoulderMargin;
                m_markerDistance = markerDistance;
                m_laneDistance = laneDistance;

                m_curOffset = m_startMargin;
            }

            public Lane FindExistingLaneFor(ITimelineNode node)
            {
                foreach (Lane curLane in m_lanes)
                {
                    if (curLane.ReservationContext == node.Context)
                        return curLane;
                }

                return null;
            }

            public Lane ReserveLaneUntil(ITimelineNode lastNode)
            {
                Lane availableLane = null;

                foreach (Lane curLane in m_lanes)
                {
                    if (curLane.IsAvailable)
                    {
                        availableLane = curLane;
                        break;
                    }
                }

                if (availableLane == null)
                {
                    availableLane = new Lane(this);
                    m_lanes.Add(availableLane);
                }

                availableLane.ReserveUntil(lastNode);

                return availableLane;
            }

            public uint PlaceMarkerStartingAt(uint timestamp, Size size)
            {
                Marker curMarker = null;

                if (m_markers.Count > 0)
                {
                    Marker lastMarker = m_markers[m_markers.Count - 1];
                    if (lastMarker.Timestamp == timestamp)
                    {
                        curMarker = lastMarker;
                    }
                    else
                    {
                        m_curOffset += lastMarker.Length + m_markerDistance;
                    }
                }

                if (curMarker == null)
                {
                    curMarker = new Marker(m_curOffset, timestamp);
                    m_markers.Add(curMarker);
                }

                curMarker.MaybeExpandToFitLength(size.Width);

                return m_curOffset;
            }

            public void UpdateLaneOffsets()
            {
                uint offset = m_shoulderMargin;

                foreach (Lane lane in m_lanes)
                {
                    lane.StartOffset = offset;
                    
                    offset += lane.Width + m_laneDistance;
                }
            }
        }

        internal class Marker
        {
            private readonly uint m_startOffset;
            private readonly uint m_timestamp;
            private uint m_length;

            public uint StartOffset
            {
                get
                {
                    return m_startOffset;
                }
            }

            public uint Timestamp
            {
                get
                {
                    return m_timestamp;
                }
            }

            public uint Length
            {
                get
                {
                    return m_length;
                }
            }

            public Marker(uint startOffset, uint timestamp)
            {
                m_startOffset = startOffset;
                m_timestamp = timestamp;
            }

            public void MaybeExpandToFitLength(uint length)
            {
                if (length > m_length)
                    m_length = length;
            }
        }

        internal class Lane
        {
            private Road m_road;

            private uint m_startOffset;
            private ITimelineNode m_lastNodeInReservation;

            private List<Car> m_cars = new List<Car>();
            private uint m_width;

            public uint StartOffset
            {
                get
                {
                    return m_startOffset;
                }

                set
                {
                    m_startOffset = value;
                }
            }

            public object ReservationContext
            {
                get
                {
                    if (m_lastNodeInReservation != null)
                        return m_lastNodeInReservation.Context;
                    else
                        return null;
                }
            }

            public bool IsAvailable
            {
                get
                {
                    return (m_lastNodeInReservation == null);
                }
            }

            public List<Car> Cars
            {
                get
                {
                    return m_cars;
                }
            }

            public uint Width
            {
                get
                {
                    return m_width;
                }
            }

            public Lane(Road road)
            {
                m_road = road;
            }

            public void ReserveUntil(ITimelineNode lastNode)
            {
                m_lastNodeInReservation = lastNode;
            }

            public void Append(ITimelineNode node)
            {
                uint offset = m_road.PlaceMarkerStartingAt(node.Timestamp, node.Allocation);

                Car car = new Car(offset, node);
                m_cars.Add(car);

                if (node.Allocation.Height > m_width)
                    m_width = node.Allocation.Height;

                if (node == m_lastNodeInReservation)
                    MakeAvailable();
            }

            private void MakeAvailable()
            {
                m_lastNodeInReservation = null;
            }
        }

        internal class Car
        {
            private uint m_offset;
            private ITimelineNode m_driver;

            public uint Offset
            {
                get
                {
                    return m_offset;
                }
            }

            public ITimelineNode Driver
            {
                get
                {
                    return m_driver;
                }
            }

            public Car(uint offset, ITimelineNode driver)
            {
                m_offset = offset;
                m_driver = driver;
            }
        }

        protected class NodeList : List<ITimelineNode>
        {
            public NodeList()
                : base()
            {
            }

            public NodeList(IEnumerable<ITimelineNode> collection)
                : base(collection)
            {
            }

            public ITimelineNode FindLastNodeWithSameContextAs(ITimelineNode node, int startIndex)
            {
                ITimelineNode lastNode = node;

                object context = node.Context;
                for (int nodeIndex = startIndex; nodeIndex < Count; nodeIndex++)
                {
                    ITimelineNode curNode = this[nodeIndex];
                    if (curNode.Context == context)
                        lastNode = curNode;
                }

                return lastNode;
            }
        }
    }
}
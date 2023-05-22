using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An helper class that position controls inside a grid.
    /// </summary>
    public class UiGridLayout
    {
        private CompUiContainer container;
        private int numRows, numColumns;
        private CompUiControl[,] controls;
        private UiHeight[] rowHeight;
        private UiWidth[] columnWidth;

        public UiGridLayout(CompUiContainer container,  int numRows, int numColumns, UiCoords topLeft)
        {
            this.numRows = numRows;
            this.numColumns = numColumns;
            this.container = container;
            this.TopLeft = topLeft;
            controls = new CompUiControl[this.numRows, this.numColumns];
            rowHeight = new UiHeight[this.numRows];
            columnWidth = new UiWidth[this.numColumns];
            SetColumnWidth("5em");
            SetRowHeight("3em");
        }

        public UiCoords TopLeft { get; set; }

        public CompUiControl this[int rowIndex, int columnIndex]
        {
            get { return controls[rowIndex, columnIndex]; }
            set { controls[rowIndex, columnIndex] = value; }
        }

        public void SetRowHeight(int rowIndex, UiHeight height)
        {
            rowHeight[rowIndex] = height;
        }

        public void SetRowHeight(UiHeight height)
        {
            for (int i = 0; i < numRows; i++)
                SetRowHeight(i, height);
        }

        public void SetColumnWidth(int columnIndex, UiWidth width)
        {
            columnWidth[columnIndex] = width;
        }

        public void SetColumnWidth(UiWidth width)
        {
            for (int i = 0; i < numColumns; i++)
                SetColumnWidth(i, width);
        }

        public void Apply()
        {
            CoordContext.Push(container.Coords);
            UiCoords rowStart = TopLeft;

            for (int iRow = 0; iRow < numRows; rowStart += rowHeight[iRow], iRow++)
            {
                List<CompUiControl> rowCtrls = new List<CompUiControl>();
                UiCoords cellPos = rowStart;
                for (int iCol = 0; iCol < numColumns; cellPos += columnWidth[iCol], iCol++)
                {
                    CompUiControl c = controls[iRow, iCol];

                    if (c == null) 
                        continue;

                    rowCtrls.Add(c);
                    c.Position = cellPos;
                }

                if (rowCtrls.Count > 1)
                    UiPositioning.AlignCenterVertically(rowCtrls.ToArray());
            }

            CoordContext.Pop();
        }

    }
}

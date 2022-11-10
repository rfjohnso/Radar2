
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SDRdue
{
    class DrawRadar : Draw
    {

        private readonly Object LockRadar = new Object();
        public float rate;
        public uint BufferSize;
        public string DeviceName;

        public GraphicsDeviceService service;
        protected SpriteBatch spriteBatch = null;
        protected SpriteFont spriteFont;
        protected BasicEffect mSimpleEffect;
        protected Texture2D texture = null;
        GraphicsHelper graphisc = new GraphicsHelper();


        protected Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        int frames = 0;
        int frames_per_sec = 0;

        float zoomX, zoomY;
        uint Rows, Columns;
        Vector2[] p;
        private int scale_columns = 10;
        private int with_colmn;
        private int zoom_half;
        private double r_b_c;
        private uint ColRow;
        private float doppler_zoom;
        private float step_y;
        private float Yscale_shift;
        private float Up;
        private float scale_rows;
        private float scaleYfactor;
        private float BufferTime;
        float ActivePlotAreaX;
        float ActivePlotAreaY;
        int Height_Bottom;
        int Width_RightMargin;
        float DistanceShift;
        int ColorTableSize;


        public void SizeChanged(Panel panelViewport, GraphicsDevice graphicsDevice, GraphicsDeviceService _service, SpriteBatch _spriteBatch, SpriteFont _spriteFont, BasicEffect _mSimpleEffect, Texture2D _texture)
        {
            service = _service;
            spriteBatch = _spriteBatch;
            spriteFont = _spriteFont;
            mSimpleEffect = _mSimpleEffect;
            texture = _texture;
            DrawPrepare(panelViewport);
        }

        //Do it once
        public void DrawPrepare(Panel panelViewport, Flags flags = null)
        {
            lock (LockRadar)
            {
                if (flags != null)
                {
                    Rows = flags.Rows;
                    Columns = flags.Columns;
                    doppler_zoom = flags.DopplerZoom;
                    DistanceShift = flags.DistanceShift;
                    BufferSize = flags.BufferSize;
                    CreateColorTable1(ColorThemeNr, flags.ColorThemeTable);
                }

                zoomX = 1.0f * panelViewport.Width / Columns;
                zoomY = 1.0f * panelViewport.Height / Rows;

                graphisc.SetValues(panelViewport, spriteBatch, texture, zoomX, zoomY);

                //Accelerators
                //Scale X
                ActivePlotAreaX = panelViewport.Width - LeftMargin - RightMargin;
                scale_columns = (int)(ActivePlotAreaX / 50);//Number of columns depends on the window size 30 the distance between columns
                with_colmn = panelViewport.Width / scale_columns;
                zoom_half = (int)zoomX / 2 + 2;

                BufferTime = ((rate / 2) / BufferSize);
                r_b_c = BufferTime * Columns * (2.0 * Math.PI) / doppler_zoom / scale_columns;
                ColRow = Columns * Rows;

                p = new Vector2[ColRow];
                Height_Bottom = panelViewport.Height - BottomMargin;
                Width_RightMargin = panelViewport.Width - RightMargin;
                uint index_i;
                float x;
                for (uint i = 0; i < Columns; i++)
                {
                    x = i * zoomX;
                    index_i = i * Rows;

                    for (uint j = 0; j < Rows; j++)
                    {
                        p[index_i + j].Y = Height_Bottom - j * zoomY;
                        p[index_i + j].X = x;
                    }
                }

                ColorTableSize = Draw.ColorTableSize - 1;

                ////////////////////////////////////////////////////////////////

                ActivePlotAreaY = panelViewport.Height - BottomMargin - TopMargin;
                //pixels
                scale_rows = 50;
                Up = ActivePlotAreaY;
                step_y = ActivePlotAreaY / scale_rows;

                //pixels to rows
                //
                //One pixel is an x rows
                float RowToPixel = Rows / ActivePlotAreaY;

                float Ls = 299792458f / 1000; //Speed of light km/s
                float km_bit = Ls / BufferSize / 2; //  km/s /b/s == km/b

                scaleYfactor = RowToPixel * km_bit;// km_bit * flags.Rows;//full rows is a max distance

                Yscale_shift = DistanceShift * scaleYfactor;


            }
        }

        public void Scene(Panel panelViewport, float[] data)
        {

            //Calculate frames per second
            frames++;
            if (watch.ElapsedMilliseconds >= 1000L)
            {
                frames_per_sec = frames;
                frames = 0;
                watch.Restart();
            }

            service.GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            //Main data plot
            int col;
            if (ColRow == data.Length)
            {
                for (uint i = 0; i < ColRow; i++)
                {
                    col = (int)(data[i]);

                    //protect
                    if (col >= ColorTableSize)
                        col = ColorTableSize;
                    if (col > 0) //just skip the black points for speed
                        graphisc.Point(p[i], ColorTable[col]);//slow
                }
            }


            this.spriteBatch.Draw(texture, new Rectangle(0, Height_Bottom + (int)graphisc.zoom, panelViewport.Width, 1), Color.White);
            string drawString;
            float x, y;

            y = Height_Bottom + (int)graphisc.zoom;

            for (int i = 1; i < scale_columns; i++)//number of pixels per column, screen scaling
            {
                x = i * with_colmn + zoom_half;

                graphisc.Line(service, mSimpleEffect, new Vector2(x, y), new Vector2(x, y + 5), graphisc.white);

                drawString = "" + ((i - scale_columns / 2) * r_b_c).ToString("0.0");
                float lt = spriteFont.MeasureString(drawString).Length() / 9;
                spriteBatch.DrawString(spriteFont, drawString, new Vector2(x - lt + 3, y + 7), Color.White, 0, new Vector2(0, 0), 0.27f, SpriteEffects.None, 0);
            }

            // spriteBatch.DrawString(spriteFont, "Doppler shift", new Vector2(panelViewport.Width / 2 - 30, panelViewport.Height + (int)graphisc.zoom - 15), Color.White, 0, new Vector2(0, 0), 0.27f, SpriteEffects.None, 0);
            spriteBatch.DrawString(spriteFont, "Radar", new Vector2(panelViewport.Width - 50, 1), graphisc.white, 0, new Vector2(0, 0), 0.3f, SpriteEffects.None, 0);

            //Additional info frames/sec
            drawString = "" + frames_per_sec + " fps   " + Rows + "x" + Columns + "    Device: " + DeviceName;
            spriteBatch.DrawString(spriteFont, drawString, new Vector2(1 + LeftMargin, 0), graphisc.white, 0, new Vector2(0, 0), 0.3f, SpriteEffects.None, 0);


            spriteBatch.End();
            spriteBatch.Begin();
            ScaleY(panelViewport.Width, panelViewport.Height);
            spriteBatch.End();

        }


        private void ScaleY(float Width, float Height)
        {
            float x = LeftMargin, y;
            string drawString;
            for (float i = scale_rows; i < Up; i += scale_rows)
            {

                drawString = "" + ((scaleYfactor * i + Yscale_shift) / BufferTime).ToString("0");
                y = Height_Bottom - i;
                //    if (y < Height - BottomMargin)
                {
                    graphisc.Line(service, mSimpleEffect, new Vector2(x, y), new Vector2(Width_RightMargin, y), graphisc.gray);
                    graphisc.Line(service, mSimpleEffect, new Vector2(x, y), new Vector2(x - 5, y), graphisc.white);
                    float lt = spriteFont.MeasureString(drawString).Length() / 2;
                    spriteBatch.DrawString(spriteFont, drawString, new Vector2(x + 18 - lt, y - 6), graphisc.white, 0, new Vector2(0, 0), 0.27f, SpriteEffects.None, 0);
                }
            }
            graphisc.Line(service, mSimpleEffect, new Vector2(LeftMargin, TopMargin), new Vector2(LeftMargin, Height_Bottom), graphisc.white);
        }

    }
}

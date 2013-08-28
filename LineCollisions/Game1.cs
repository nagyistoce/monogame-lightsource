#region Using Statements
using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace LineCollisions
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        BasicEffect lineEffect;
        MouseState mouseLast = new MouseState();
        MouseState mouseCurr;
        KeyboardState kbLast = new KeyboardState();
        KeyboardState kbCurr;

        List<Line> lines;
        List<Vert> verts;
        Texture2D coll;
        Texture2D lightSpot;
        Texture2D pixel;
        SpriteFont spf;
        Vector2 lightStart = new Vector2(6 * 32, 13 * 32);
        List<Vector2> debugDraw = new List<Vector2>();
        readonly double RayToSegMult = Math.Pow(2, 14);
        float umbraScale = 0.2f;
        int extraUmbraSize = 1024;
        bool debugCollinearIntersectionEnabled = true;
        bool debugDrawCasts = false;
        bool debugDrawVertOrder = true;
        bool debugDrawLinesCount = false;
        bool debugDrawHits = false;
        bool debugDrawHitsPos = false;
        bool debugDrawBeforeAfter = true;
        bool debugDrawMeshMarks = true;
        bool debugDrawMeshMarksOrder = false;
        bool debugCornerCaseOneFix = false;
        bool debugPrintDifferences = false;
        bool debugPrintMarkVPC = false;

        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            coll = this.Content.Load<Texture2D>("kawaii.png");
            lightSpot = this.Content.Load<Texture2D>("light.png");
            spf = this.Content.Load<SpriteFont>("dosis");

            pixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixel.SetData<Color>(new Color[] { Color.White });

            lineEffect = new BasicEffect(GraphicsDevice);
            lineEffect.VertexColorEnabled = true;
            lineEffect.Projection = Matrix.CreateOrthographicOffCenter(0f, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 0f, 0f, 1f);
            lineEffect.View = Matrix.Identity;
            lineEffect.World = Matrix.Identity;

            lines = new List<Line>();
            verts = new List<Vert>();

            //MakeMap();

            LoadMap();
        }

        private void LoadMap()
        {
            XDocument xdoc = XDocument.Load("maptest.tmx");
            var objectGroup = xdoc.Descendants("map").First().Descendants("objectgroup").First();
            var objects = objectGroup.Descendants("object");

            //for every object
            foreach (var obj in objects)
            {
                int objectX = int.Parse(obj.Attribute("x").Value);
                int objectY = int.Parse(obj.Attribute("y").Value);
                string polylinePoints = obj.Descendants("polyline").First().Attribute("points").Value;

                string[] pairs = polylinePoints.Split(' ');

                Vector2 lastPoint = new Vector2(0, 0);
                for (int ip = 0; ip < pairs.Length; ip++)
                {
                    string pair = pairs[ip];
                    int thisX = int.Parse(pair.Split(',')[0]);
                    int thisY = int.Parse(pair.Split(',')[1]);

                    Vector2 thisPoint = new Vector2(thisX + objectX, thisY + objectY);

                    if (ip != 0) //this isn't the first point, so we can make a line from last to this.
                        AddLine(lastPoint, thisPoint);

                    lastPoint = thisPoint;
                }
            }

            AssignLinesToVerts();
        }

        private void AddLine(Vector2 fromPos, Vector2 toPos)
        {
            //find a vert in verts whose position matches "from"
            Vert matchingFrom = FindInList(fromPos);

            //if there are none there, create one, add it to the list
            if (matchingFrom == null)
            {
                matchingFrom = new Vert(fromPos);
                verts.Add(matchingFrom);
            }

            //do the same with "to"
            Vert matchingTo = FindInList(toPos);

            if (matchingTo == null)
            {
                matchingTo = new Vert(toPos);
                verts.Add(matchingTo);
            }

            //with these verts, construct the line
            Line thisLine = new Line(matchingFrom, matchingTo);
            //and add it to lines
            lines.Add(thisLine);
        }

        private Vert FindInList(Vector2 itsPos)
        {
            foreach (Vert v in verts)
            {
                if (v.position.Equals(itsPos))
                    return v;
            }

            return null;
        }

        private void MakeMap()
        {
            verts.AddRange(new Vert[]
            {
                new Vert(new Vector2(32,32)),
                new Vert(new Vector2(800-32, 32)),
                new Vert(new Vector2(800-32,600-32)),
                new Vert(new Vector2(32,600-32)),
                
                new Vert(new Vector2(237,191)),
                new Vert(new Vector2(354,264)),
                new Vert(new Vector2(470,198)),
                new Vert(new Vector2(543,242)),
                new Vert(new Vector2(369,335)),
                new Vert(new Vector2(304,389)), 

                //new Vert(new Vector2(140,172)),
                //new Vert(new Vector2(219,170)),
                //new Vert(new Vector2(143,229)),
                //new Vert(new Vector2(219,232)),
                //new Vert(new Vector2(142,308)),
                
                //new Vert(new Vector2(282,175)),
                //new Vert(new Vector2(283,280)),
                //new Vert(new Vector2(336,314)),
                //new Vert(new Vector2(373,289)),
                //new Vert(new Vector2(381,180)),
                
                //new Vert(new Vector2(532,180)),
                //new Vert(new Vector2(483,179)),
                //new Vert(new Vector2(445,222)),
                //new Vert(new Vector2(447,288)),
                //new Vert(new Vector2(486,325)),
                //new Vert(new Vector2(536,323)),

                //new Vert(new Vector2(603,177)),
                //new Vert(new Vector2(608,259)),
                //new Vert(new Vector2(610,327)),
                //new Vert(new Vector2(672,182)),
                //new Vert(new Vector2(679,338)),
            });

            lines.AddRange(new Line[]
            {
                new Line(verts[0], verts[1]), //wall
                new Line(verts[1], verts[2]),
                new Line(verts[2], verts[3]),
                new Line(verts[3], verts[0]),

                new Line(verts[4], verts[5]),
                new Line(verts[5], verts[6]),
                new Line(verts[6], verts[7]),
                new Line(verts[7], verts[8]),
                new Line(verts[8], verts[9]),
                new Line(verts[9], verts[4]),

                //new Line(verts[4], verts[5]), //
                //new Line(verts[6], verts[7]),
                //new Line(verts[4], verts[8]),

                //new Line(verts[9], verts[10]), //
                //new Line(verts[10], verts[11]),
                //new Line(verts[11], verts[12]),
                //new Line(verts[12], verts[13]),

                //new Line(verts[14], verts[15]), //
                //new Line(verts[15], verts[16]),
                //new Line(verts[16], verts[17]),
                //new Line(verts[17], verts[18]),
                //new Line(verts[18], verts[19]),
                
                //new Line(verts[20], verts[22]), //
                //new Line(verts[21], verts[23]),
                //new Line(verts[21], verts[24]),
            });

            //assign lines to verts
            AssignLinesToVerts();
        }

        private void AssignLinesToVerts()
        {
            for (int line = 0; line < lines.Count; line++)
            {
                var l = lines[line];
                l.start.lines.Add(l);
                l.end.lines.Add(l);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            mouseCurr = Mouse.GetState();
            kbCurr = Keyboard.GetState();

            //corner case line intersection test
            if (kbCurr.IsKeyDown(Keys.Left))
                lightStart.X -= 4f;
            if (kbCurr.IsKeyDown(Keys.Right))
                lightStart.X += 4f;
            if (kbCurr.IsKeyDown(Keys.Up))
                lightStart.Y -= 4f;
            if (kbCurr.IsKeyDown(Keys.Down))
                lightStart.Y += 4f;

            if (KeyboardPressed(Keys.P))
                Console.WriteLine(lightStart);
            if (KeyboardPressed(Keys.C))
            {
                debugCollinearIntersectionEnabled = !debugCollinearIntersectionEnabled; //toggle
                Console.WriteLine("collinearIntersectionEnabled: " + debugCollinearIntersectionEnabled);
            }
            if (KeyboardPressed(Keys.F))
            {
                debugCornerCaseOneFix = !debugCornerCaseOneFix;
                Console.WriteLine("debugCornerCaseOneFix: " + debugCornerCaseOneFix);
            }
            debugPrintDifferences = false; //just one frame we want it to print.
            if (KeyboardPressed(Keys.D))
            {
                debugPrintDifferences = true;
                Console.WriteLine("DIFFS:");
            }
            if (KeyboardPressed(Keys.R))
            {
                debugDrawCasts = !debugDrawCasts;
                Console.WriteLine("debugDrawCasts: " + debugDrawCasts);
            }
            debugPrintMarkVPC = false;
            if (KeyboardPressed(Keys.M))
            {
                debugPrintMarkVPC = true;
            }

            mouseLast = mouseCurr;
            kbLast = kbCurr;
            base.Update(gameTime);
        }

        public bool MousePressed(int which)
        {
            switch (which)
            {
                case 1:
                    return mouseCurr.LeftButton == ButtonState.Pressed && mouseLast.LeftButton == ButtonState.Released;
                case 2:
                    return mouseCurr.RightButton == ButtonState.Pressed && mouseLast.RightButton == ButtonState.Released;
                case 3:
                    return mouseCurr.MiddleButton == ButtonState.Pressed && mouseLast.MiddleButton == ButtonState.Released;
            }

            return false;
        }

        public bool MouseReleased(int which)
        {
            switch (which)
            {
                case 1:
                    return mouseCurr.LeftButton == ButtonState.Released && mouseLast.LeftButton == ButtonState.Pressed;
                case 2:
                    return mouseCurr.RightButton == ButtonState.Released && mouseLast.RightButton == ButtonState.Pressed;
                case 3:
                    return mouseCurr.MiddleButton == ButtonState.Released && mouseLast.MiddleButton == ButtonState.Pressed;
            }

            return false;
        }

        public bool KeyboardPressed(Keys which)
        {
            return kbCurr.IsKeyDown(which) && kbLast.IsKeyUp(which);
        }

        public bool KeyboardReleased(Keys which)
        {
            return kbCurr.IsKeyUp(which) && kbLast.IsKeyDown(which);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            lineEffect.CurrentTechnique.Passes[0].Apply();

            //DRAW LIGHTMAP
            DrawLightmap();

            //draw all lines
            foreach (Line l in lines)
            {
                VertexPositionColor[] vpc = new VertexPositionColor[]
                    {
                        new VertexPositionColor(new Vector3(l.start.position, 0f), Color.Red),
                        new VertexPositionColor(new Vector3(l.end.position, 0f), Color.Red)
                    };

                GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, vpc, 0, 2, new int[] { 0, 1 }, 0, 1, VertexPositionColor.VertexDeclaration);
            }

            //draw umbra
            //left
            Rectangle umbraLeft = new Rectangle((int)(lightStart.X - (lightSpot.Width * umbraScale * 0.5f)) - extraUmbraSize,
                                                (int)(lightStart.Y - (lightSpot.Height * umbraScale * 0.5f)) - extraUmbraSize,
                                                extraUmbraSize,
                                                (int)(lightSpot.Height * umbraScale) + (extraUmbraSize * 2));
            //right
            Rectangle umbraRight = new Rectangle((int)(lightStart.X + (lightSpot.Width * umbraScale * 0.5f)),
                                                (int)(lightStart.Y - (lightSpot.Height * umbraScale * 0.5f)) - extraUmbraSize,
                                                extraUmbraSize,
                                                (int)(lightSpot.Height * umbraScale) + (extraUmbraSize * 2));
            //top
            Rectangle umbraTop = new Rectangle((int)(lightStart.X - (lightSpot.Width * umbraScale * 0.5f)),
                                               (int)(lightStart.Y - (lightSpot.Height * umbraScale * 0.5f)) - extraUmbraSize,
                                               (int)(lightSpot.Width * umbraScale),
                                               extraUmbraSize);
            //bottom
            Rectangle umbraBottom = new Rectangle((int)(lightStart.X - (lightSpot.Width * umbraScale * 0.5f)),
                                                  (int)(lightStart.Y + (lightSpot.Height * umbraScale * 0.5f)),
                                                  (int)(lightSpot.Width * umbraScale),
                                                  extraUmbraSize);
            //spriteBatch.Draw(lightSpot, lightStart, null, Color.White, 0f, new Vector2(lightSpot.Width / 2f, lightSpot.Height / 2f), umbraScale, SpriteEffects.None, 0.2f);
            //spriteBatch.Draw(pixel, umbraLeft, Color.Black);
            //spriteBatch.Draw(pixel, umbraRight, Color.Black);
            //spriteBatch.Draw(pixel, umbraTop, Color.Black);
            //spriteBatch.Draw(pixel, umbraBottom, Color.Black);

            //draw light
            spriteBatch.Draw(coll, new Rectangle((int)lightStart.X, (int)lightStart.Y, 16, 16), null, Color.White, 0f, new Vector2(16, 16), SpriteEffects.None, 0f);

            spriteBatch.End();
            base.Draw(gameTime);
        }

        public void DrawLightmap()
        {
            debugDraw.Clear();

            //get verts in order of angle
            List<Vert> vertsInOrderOfAngle = /*verts.OrderBy(ve => (Math.Atan2(-(ve.position.Y - lightStart.Y), -(ve.position.X - lightStart.X)) + Math.PI))
                                                  .ThenBy(ve => ve.position.Y).ToList();*/
                                           (from ve in verts
                                            orderby Math.Atan2(-(ve.position.Y - lightStart.Y), -(ve.position.X - lightStart.X)) + Math.PI
                                                    //,Math.Atan2(-(ve.position.Y - lightStart.Y + -0.000003f), -(ve.position.X - lightStart.X + 0.000007f)) + Math.PI
                                                    ,1.0d/Vector2.DistanceSquared(ve.position, lightStart)
                                            select ve).ToList();
            //fuck atan2s domain. so retarded. why would anyone use anything other than 0 to 360. oh well. this fixes the domain.

            //for holding the marked lightmap triangle endpoints
            List<Vector2> lightmapMark = new List<Vector2>();

            //for each vert in order of angle:
            for (int vert = 0; vert < vertsInOrderOfAngle.Count; vert++)
            {
                //this vert
                Vert vertTarget = vertsInOrderOfAngle[vert];

                //make a ray
                double angleBetweenHereAndThere = Math.Atan2(vertTarget.position.Y - lightStart.Y, vertTarget.position.X - lightStart.X);

                if (angleBetweenHereAndThere < 0d) angleBetweenHereAndThere += MathHelper.TwoPi;

                Vector2 here = lightStart;
                Vector2 there = new Vector2(
                    (float)(Math.Cos(angleBetweenHereAndThere) * RayToSegMult) + here.X,
                    (float)(Math.Sin(angleBetweenHereAndThere) * RayToSegMult) + here.Y
                );
                Line rayToThere = new Line(here, there);

                //draw the raycast
                if (debugDrawCasts)
                {
                    VertexPositionColor[] vpc = new VertexPositionColor[]
                        {
                            new VertexPositionColor(new Vector3(rayToThere.start.position, 0f), Color.Aqua),
                            new VertexPositionColor(new Vector3(rayToThere.end.position, 0f), Color.Aqua)
                        };
                    GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, vpc, 0, 2, new int[] { 0, 1 }, 0, 1, VertexPositionColor.VertexDeclaration);
                }

                //draw order of vert
                if (debugDrawVertOrder)
                {
                    spriteBatch.DrawString(spf, vert.ToString(), vertTarget.position, Color.Gold);
                }

                //draw number of lines per vert
                if (debugDrawLinesCount)
                {
                    spriteBatch.DrawString(spf, vertTarget.lines.Count.ToString(), vertTarget.position, Color.Gold);
                }

                //cast each ray, ignoring lines that the target vert is a part of (eliminates corner case)
                CastResult castResult = RayCast(rayToThere, vertTarget.lines);
                var hits = castResult.GetHits(lightStart);

                //add each hit Vector2 to a debug draw list
                if (debugDrawHits)
                {
                    for (int hit = 0; hit < hits.Count; hit++)
                    {
                        var hitVector = hits[hit].Item2;
                        debugDraw.Add(hitVector);
                    }
                }

                /* /\/\/\ NOW, WE MARK /\/\/\ */

                //either out of bounds or double sided target (meaning a logical map corner is passed through)
                if (hits.Count == 0)
                {
                    lightmapMark.Add(vertTarget.position);
                    continue;
                }

                var targetLocation = vertTarget.position;
                var firstHitLocation = hits[0].Item2;

                //if didn't even reach target
                if (Vector2.DistanceSquared(lightStart, firstHitLocation) < Vector2.DistanceSquared(lightStart, targetLocation))
                    continue; //go to next vert

                //find out if the target has lines before or after.
                bool targetHasLinesBefore = false;
                bool targetHasLinesAfter = false;

                if (debugPrintDifferences)
                    Console.WriteLine("---- " + vert);

                for (int ci = 0; ci < vertTarget.lines.Count; ci++)
                {
                    Line connectedLine = vertTarget.lines[ci];
                    Vert counterpoint = vertTarget.Equals(connectedLine.start) ? connectedLine.end : connectedLine.start;
                    double angleBetweenTargetAndOther = Math.Atan2(vertTarget.position.Y - counterpoint.position.Y, vertTarget.position.X - counterpoint.position.X);
                    if (angleBetweenTargetAndOther < 0d) angleBetweenTargetAndOther += MathHelper.TwoPi;

                    double difference = angleBetweenTargetAndOther - angleBetweenHereAndThere;
                    if (difference < 0d) difference += MathHelper.TwoPi;

                    if (debugPrintDifferences)
                        Console.WriteLine(MathHelper.ToDegrees((float)difference));

                    if ((difference == 0d || difference == Math.PI) && debugCornerCaseOneFix) //"collinear" corner case. see below.
                    {
                        targetHasLinesAfter = false;
                        targetHasLinesBefore = false;
                    }
                    else
                    {
                        if (difference >= Math.PI)
                            targetHasLinesAfter = true;
                        else
                            targetHasLinesBefore = true;
                    }
                }

                //draw whether target has lines before or after
                if (debugDrawBeforeAfter)
                {
                    string lr = "";
                    if (targetHasLinesBefore)
                        lr += "b";
                    if (targetHasLinesAfter)
                        lr += "a";
                    spriteBatch.DrawString(spf, lr, vertTarget.position, Color.Violet);
                }

                //just before
                if (targetHasLinesBefore && !targetHasLinesAfter)
                {
                    lightmapMark.Add(targetLocation);
                    lightmapMark.Add(firstHitLocation);
                    continue;
                }
                //just after
                if (targetHasLinesAfter && !targetHasLinesBefore)
                {
                    lightmapMark.Add(firstHitLocation);
                    lightmapMark.Add(targetLocation);
                    continue;
                }
                //before and after
                if (targetHasLinesBefore && targetHasLinesAfter)
                {
                    lightmapMark.Add(targetLocation);
                }
                if (!targetHasLinesAfter && !targetHasLinesBefore && debugCornerCaseOneFix) //fucking corner case. see cornercase_beforeafter.png
                {
                    lightmapMark.Add(firstHitLocation);
                }

            }

            //draw the debugdraws
            if (debugDrawHits)
            {
                for (int debugs = 0; debugs < debugDraw.Count; debugs++)
                {
                    spriteBatch.Draw(coll, new Rectangle((int)debugDraw[debugs].X, (int)debugDraw[debugs].Y, 16, 16), null, Color.White, 0f, new Vector2(16, 16), SpriteEffects.None, 0f);
                    if (debugDrawHitsPos)
                        spriteBatch.DrawString(spf, debugDraw[debugs].X + "," + debugDraw[debugs].Y, debugDraw[debugs], Color.Blue);
                }
            }

            //draw the marks
            if (debugDrawMeshMarks)
            {
                for (int marks = 0; marks < lightmapMark.Count; marks++)
                {
                    spriteBatch.Draw(coll, new Rectangle((int)lightmapMark[marks].X, (int)lightmapMark[marks].Y, 16, 16), null, Color.Fuchsia, 0f, new Vector2(16, 16), SpriteEffects.None, 0f);
                    if (debugDrawMeshMarksOrder)
                        spriteBatch.DrawString(spf, marks.ToString(), lightmapMark[marks], Color.Blue);
                }
            }

            /* /\/\/\ NOW, WE TURN THE MARKS INTO A LIGHTMAP /\/\/\ */
            if (lightmapMark.Count == 0) return;

            lightmapMark.Add(lightmapMark.First());

            //the last one will contain the center light
            VertexPositionColor[] markvpc = new VertexPositionColor[lightmapMark.Count + 1];
            for (int tri = 0; tri < lightmapMark.Count; tri++)
            {
                Vector3 position = new Vector3(lightmapMark[tri], 0);
                Color color = new Color(22, 48, 99, 10);
                markvpc[tri] = new VertexPositionColor(position, color);
            }

            //the center light
            markvpc[markvpc.Length - 1] = new VertexPositionColor(new Vector3(lightStart, 0), new Color(22, 48, 99, 10));

            //print marks in order
            if (debugPrintMarkVPC)
            {
                Console.WriteLine("MARK ORDER: ");
                for (int i = 0; i < markvpc.Length; i++)
                {
                    Console.WriteLine(markvpc[i].Position);
                }
            }

            //generate index
            int centerLightIndex = markvpc.Length - 1;
            List<int> indexData = new List<int>(lightmapMark.Count * 3);

            for (int vert = 0; vert < markvpc.Length - 1; vert++)
            {
                if (vert != 0)                      //if not first
                    indexData.Add(vert);            //mark vert
                if (vert != markvpc.Length - 2)     //if not last
                {
                    indexData.Add(centerLightIndex); //mark center
                    indexData.Add(vert);            //mark vert
                }
            }

            GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, markvpc, 0, markvpc.Length, indexData.ToArray(), 0, indexData.Count / 3, VertexPositionColor.VertexDeclaration);
        }

        public CastResult RayCast(Line ray, List<Line> ignore)
        {
            CastResult thisRayHits = new CastResult();

            //for each line, check intersect
            for (int l = 0; l < lines.Count; l++)
            {
                //if ignorelist contains lines[l], continue
                if (ignore.Contains(lines[l])) continue;

                var hitTest = LineIntersection(ray, lines[l]);

                if (hitTest != null)
                    thisRayHits.AddHit(lines[l], (Vector2)hitTest);
            }

            return thisRayHits;
        }

        public Vector2? LineIntersection(Line AB, Line CD)
        {
            double deltaACy = AB.start.position.Y - CD.start.position.Y;
            double deltaDCx = CD.end.position.X - CD.start.position.X;
            double deltaACx = AB.start.position.X - CD.start.position.X;
            double deltaDCy = CD.end.position.Y - CD.start.position.Y;
            double deltaBAx = AB.end.position.X - AB.start.position.X;
            double deltaBAy = AB.end.position.Y - AB.start.position.Y;

            double denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
            double numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

            if (denominator == 0)
            {
                if (numerator == 0)
                {
                    // collinear. Potentially infinite intersection points.
                    // Check and return one of them.
                    if (debugCollinearIntersectionEnabled)
                    {
                        if (AB.start.position.X >= CD.start.position.X && AB.start.position.X <= CD.end.position.X)
                        {
                            return AB.start.position;
                        }
                        else if (CD.start.position.X >= AB.start.position.X && CD.start.position.X <= AB.end.position.X)
                        {
                            return CD.start.position;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return null;
                }
                else
                { // parallel
                    return null;
                }
            }

            double r = numerator / denominator;
            if (r < 0 || r > 1)
            {
                return null;
            }

            double s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
            if (s < 0 || s > 1)
            {
                return null;
            }

            return new Vector2((float)(AB.start.position.X + r * deltaBAx), (float)(AB.start.position.Y + r * deltaBAy));
        }
    }

    public class CastResult
    {
        List<Tuple<Line, Vector2>> hits;

        public CastResult()
        {
            hits = new List<Tuple<Line, Vector2>>();
        }

        public List<Tuple<Line, Vector2>> GetHits(Vector2 lightSource)
        {
            return (from hit in hits
                    orderby Vector2.DistanceSquared(lightSource, hit.Item2)
                    select hit).ToList();
        }

        public void AddHit(Line which, Vector2 atWhere)
        {
            hits.Add(new Tuple<Line, Vector2>(which, atWhere));
        }
    }

    public class Line
    {
        public Vert start;
        public Vert end;

        public Line(Vert a, Vert b)
        {
            this.start = a;
            this.end = b;
        }

        public Line(Vector2 a, Vector2 b)
        {
            this.start = new Vert(a);
            this.start.lines.Add(this);
            this.end = new Vert(b);
            this.end.lines.Add(this);
        }
    }

    public class Vert
    {
        public Vector2 position;
        public List<Line> lines;

        public Vert(Vector2 pos)
        {
            position = pos;
            lines = new List<Line>();
        }
    }
}

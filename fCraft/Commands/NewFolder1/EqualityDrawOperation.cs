/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

//Copyright (C) <2011 - 2013> Lao Tszy (lao_tszy@yahoo.co.uk)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace fCraft
{
	//draws volume, defined by an inequality 
	public class EqualityDrawOperation : DrawOperation
	{
		private Expression _expression;
		private Scaler _scaler;
		private int _count;
		public EqualityDrawOperation(Player player, CommandReader cmd)
			: base(player)
		{
			string strFunc = cmd.Next();
            if (string.IsNullOrWhiteSpace(strFunc))
            {
                player.Message("empty equality expression");
                return;
            }
            if (strFunc.Length < 3)
            {
                player.Message("expression is too short (should be like f(x,y,z)=g(x,y,z))");
                return;
            }

			strFunc = strFunc.ToLower();

			_expression = SimpleParser.ParseAsEquality(strFunc, new string[] { "x", "y", "z" });
			
			player.Message("Expression parsed as " + _expression.Print());
			string scalingStr = cmd.Next();
			_scaler = new Scaler(scalingStr);
		}

		public override int DrawBatch(int maxBlocksToDraw)
		{
			//ignoring maxBlocksToDraw

			//do it 3 times, iterating axis in different order, to get to the closed surface as close as possible
			InternalDraw(ref Coords.X, ref Coords.Y, ref Coords.Z,
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						ref Coords.X, ref Coords.Y, ref Coords.Z,
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						maxBlocksToDraw);
			InternalDraw(ref Coords.X, ref Coords.Z, ref Coords.Y,
						Bounds.XMin, Bounds.XMax, Bounds.ZMin, Bounds.ZMax, Bounds.YMin, Bounds.YMax,
						ref Coords.X, ref Coords.Y, ref Coords.Z,
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						maxBlocksToDraw);
			InternalDraw(ref Coords.Y, ref Coords.Z, ref Coords.X,
						Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax, Bounds.XMin, Bounds.XMax,
						ref Coords.X, ref Coords.Y, ref Coords.Z,
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						maxBlocksToDraw);
			
			IsDone = true;
			return _count;
		}

		//this method exists to box coords nicely as ref params, note that the set of {arg1, arg2, arg3} must be the same with 
		//{ xArg, yArg, zArg }
		private int InternalDraw(ref int arg1, ref int arg2, ref int arg3, 
			int min1, int max1, int min2, int max2, int min3, int max3, 
			ref int argX, ref int argY, ref int argZ, 
			int minX, int maxX, int minY, int maxY, int minZ, int maxZ, 
			int maxBlocksToDraw)
		{
			_count = 0;
			int exCount = 0;

            for (arg1 = min1; arg1 <= max1 && BuildingCommands.MaxCalculationExceptions >= exCount; ++arg1)
			{
                for (arg2 = min2; arg2 <= max2 && BuildingCommands.MaxCalculationExceptions >= exCount; ++arg2)
				{
					double prevDiff = 0;
					double prevComp = 0;
					for (int arg3Iterator = min3; arg3Iterator <= max3; ++arg3Iterator)
					{
						try
						{
							arg3 = arg3Iterator;
							Tuple<double, double> res=
								_expression.EvaluateAsEquality(_scaler.ToFuncParam(argX, minX, maxX),
									                     _scaler.ToFuncParam(argY, minY, maxY),
														 _scaler.ToFuncParam(argZ, minZ, maxZ));
							//decision: we cant only take points with 0 as comparison result as it will happen almost never.
							//We are reacting on the changes of the comparison result sign 
							arg3 = int.MaxValue;
							if (res.Item1 == 0) //exactly equal, wow, such a surprise
								arg3 = arg3Iterator;
							else if (res.Item1 * prevComp < 0) //i.e. different signs, but not the prev==0
								arg3 = res.Item2 < prevDiff ? arg3Iterator : arg3Iterator - 1; //then choose the closest to 0 difference
							
							if (DrawOneBlock())
								++_count;
							//if (TimeToEndBatch)
							//    return _count;

							prevComp = res.Item1;
                            prevDiff = res.Item2;
						}
						catch (Exception)
						{
							//the exception here is kinda of normal, for functions (especially interesting ones)
							//may have eg punctured points; we just have to keep an eye on the number, since producing 10000
							//exceptions in the multiclient application is not the best idea
                            if (++exCount > BuildingCommands.MaxCalculationExceptions)
							{
                                Player.Message("Surface drawing is interrupted: too many (>" + BuildingCommands.MaxCalculationExceptions +
								               ") calculation exceptions.");
								break;
							}
						}
					}
				}
			}
			return _count;
		}

		public override bool Prepare(Vector3I[] marks)
		{
			if (!base.Prepare(marks))
			{
				return false;
			}
			BlocksTotalEstimate = Bounds.Volume;
			return true;
		}
		public override string Name
		{
			get
			{
				return "Equality";
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using System.Diagnostics;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Quadrants
{
	/// <summary>
	/// Monitors opposing lane to the rear of us
	/// </summary>
	public class OpposingRearQuadrantMonitor
	{
		/// <summary>
		/// Checks time since lateral vehicle clear
		/// </summary>
		public Stopwatch RearClearStopwatch;

		/// <summary>
		/// Current vehicle we're tracking
		/// </summary>
		public VehicleAgent CurrentVehicle;

		/// <summary>
		/// distance to vehicle
		/// </summary>
		private double currentDistance;

		/// <summary>
		/// current lane
		/// </summary>
		private ArbiterLane lane;

		/// <summary>
		/// SIde of the vehicle
		/// </summary>
		public SideObstacleSide VehicleSide;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public OpposingRearQuadrantMonitor(ArbiterLane lane, SideObstacleSide sos)
		{
			this.lane = lane;
			this.VehicleSide = sos;
			this.RearClearStopwatch = new Stopwatch();
		}

		/// <summary>
		/// Checks if it is clear to go into the other lane
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public bool IsClear(VehicleState state, double vUs)
		{
			// temp get
			bool b = this.CheckClear(state, vUs);

			if (!b)
			{
				this.ResetTiming();
				return false;
			}

			// if none found, tiemr not running start timer
			if (!this.RearClearStopwatch.IsRunning)
			{
				this.ResetTiming();
				this.RearClearStopwatch.Start();
				ArbiterOutput.Output("Opposing Rear: " + this.VehicleSide.ToString() + " Clear, starting cooldown");
				return false;
			}
			// enough time
			else if (this.RearClearStopwatch.IsRunning && this.RearClearStopwatch.ElapsedMilliseconds / 1000.0 > 2.0)
			{
				ArbiterOutput.Output("Opposing Rear: " + this.VehicleSide.ToString() + " Clear, cooldown complete");
				return true;
			}
			// not enough time
			else
			{
				double timer = this.RearClearStopwatch.ElapsedMilliseconds / 1000.0;
				ArbiterOutput.Output("Opposing Rear: " + this.VehicleSide.ToString() + " Clear, cooldown timer: " + timer.ToString("F2"));
				return false;
			}
		}

		/// <summary>
		/// Checks if the rear of us is clear
		/// </summary>
		/// <param name="state"></param>
		/// <param name="vUs"></param>
		/// <returns></returns>
		private bool CheckClear(VehicleState state, double vUs)
		{
			// check if the rear vehicle exists and is moving along with us
			if (this.CurrentVehicle != null)
			{
				// check that he is more than 1vl behind us
				if (currentDistance > TahoeParams.VL)
				{
					// if hes going wrong direction then no matter
					if (this.CurrentVehicle.VehicleDirectionAlong(lane) == VehicleDirectionAlongPath.Reverse)
					{
						// params
						double vOther = CurrentVehicle.Speed;
						double xSeparation = currentDistance;

						// if we're going slower than them it actually matters
						if (vUs < vOther)
						{
							// get distance of envelope for him to slow to our speed
							double xEnvelope = (Math.Pow(vUs, 2.0) - Math.Pow(vOther, 2.0)) / (2.0 * -0.5);

							// if we're stopped
							if (vUs < 1.0)
							{
								// check to see if vehicle is outside of the envelope to slow down for us after 3 seconds
								double xSafe = xSeparation - TahoeParams.VL - (xEnvelope + (vOther * 3.0));
								return xSafe > 0 ? true : false;
							}
							else
							{
								// check to see if vehicle is outside of the envelope to slow down for us
								double xSafe = xSeparation - TahoeParams.VL - (xEnvelope + vOther);
								return xSafe > 0 ? true : false;
							}
						}
						else
							return true;
					}
					else
						return true;
				}
				else
					return false;
			}
			else
				return true;
		}

		/// <summary>
		/// Update the rear monitor with the closest vehicle in the rear
		/// </summary>
		/// <param name="state"></param>
		public void Update(VehicleState state)
		{
			// get the forward path
			LinePath p = lane.LanePath();

			// get our position
			Coordinates f = state.Front - state.Heading.Normalize(TahoeParams.VL);

			// get all vehicles associated with those components
			List<VehicleAgent> vas = new List<VehicleAgent>();
			foreach (IVehicleArea iva in lane.AreaComponents)
			{
				if (TacticalDirector.VehicleAreas.ContainsKey(iva))
					vas.AddRange(TacticalDirector.VehicleAreas[iva]);
			}

			// get the closest forward of us
			double minDistance = Double.MaxValue;
			VehicleAgent closest = null;

			// get clsoest
			foreach (VehicleAgent va in vas)
			{
				// get position of front
				Coordinates frontPos = va.ClosestPosition;
				double frontDist = lane.DistanceBetween(f, frontPos);

				if (frontDist >= 0 && frontDist < minDistance)
				{
					minDistance = frontDist;
					closest = va;
				}
			}

			this.CurrentVehicle = closest;
			this.currentDistance = minDistance;
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
			this.CurrentVehicle = null;
			this.currentDistance = 0.0;
			this.ResetTiming();
		}

		/// <summary>
		/// REasets clear stopwatch
		/// </summary>
		private void ResetTiming()
		{
			this.RearClearStopwatch.Stop();
			this.RearClearStopwatch.Reset();
		}
	}
}

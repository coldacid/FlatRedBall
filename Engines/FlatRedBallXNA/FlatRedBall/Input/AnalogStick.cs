using System;
using System.Collections.Generic;
using System.Text;

//#if SILVERLIGHT
//using SilverArcade.SilverSprite;
//using Vector2 = SilverArcade.SilverSprite.Vector2;
//#else 
using Microsoft.Xna.Framework;
using FlatRedBall.Math;
//#endif

namespace FlatRedBall.Input
{
    #region XML Docs
    /// <summary>
    /// A two-axis input device which can return a range of values on both axes.
    /// </summary>
    #endregion
    public class AnalogStick : I2DInput
    {
        #region Fields

        Vector2 mPosition;
        Vector2 mVelocity;
        double mMagnitude;
        double mAngle;

        AnalogButton leftAsButton;
        AnalogButton rightAsButton;
        AnalogButton upAsButton;
        AnalogButton downAsButton;

        #region XML Docs
        /// <summary>
        /// The DPadOnValue and DPadOffValue
        /// values are used to simulate D-Pad control
        /// with the analog stick.  When the user is above
        /// the absolute value of the mDPadOnValue then it is
        /// as if the DPad is held down.  To release the DPad the
        /// value must come under the off value.  If there was only
        /// one value then the user could hold the stick near the threshold
        /// and get rapid on/off values due to the inaccuracy of the analog stick. 
        /// </summary>
        #endregion
        internal const float DPadOnValue = .550f;
        internal const float DPadOffValue = .450f;

        bool[] mLastDPadDown = new bool[4];
        bool[] mCurrentDPadDown = new bool[4];

        /* The following would be useful to add
         * float mAngularVelocity;
         * float mDeadzone;
         */

        // Start this at -1 instead of 0, otherwise the first frame will return "true" because
        // the last push on time 0 will match the TimeManager.CurrentTime
        double[] mLastDPadPush = new double[4]{ -1, -1, -1, -1};
        double[] mLastDPadRepeatRate = new double[4] { -1, -1, -1, -1 };

        #endregion

        #region Properties

        /// <summary>
        /// Returns the left direction of the analog stick as an AnalogButton instance.
        /// </summary>
        /// <remarks>
        /// Value range is 0 to 1.
        /// Value if analog stick is held all the way to the left is 1. Note that
        /// the value is positive in this case, which is the opposite of the AnalogStick's Position.X.
        /// Value if analog stick is in neutral position is 0.
        /// Value if analog stick is held all the way to the right is still 0.
        /// </remarks>
        public AnalogButton LeftAsButton
        {
            get { return leftAsButton; }
        }

        /// <summary>
        /// Returns the right direction of the analog stick as an AnalogButton instance.
        /// </summary>
        /// <remarks>
        /// Value range is 0 to 1.
        /// Value if analog stick is held all the way to the left is 0.
        /// Value if analog stick is in neutral position is 0.
        /// Value if analog stick is held all the way to the right is 1.
        /// </remarks>
        public AnalogButton RightAsButton
        {
            get { return rightAsButton; }
        }

        /// <summary>
        /// Returns the up direction of the analog stick as an AnalogButton instance.
        /// </summary>
        /// <remarks>
        /// Value range is 0 to 1.
        /// Value if analog stick is held all the way downward is 0.
        /// Value if analog stick is in neutral position is 0.
        /// Value if analog stick is held all the way upward is 1.
        /// </remarks>
        public AnalogButton UpAsButton
        {
            get { return upAsButton; }
        }

        /// <summary>
        /// Returns the down direction of the analog stick as an AnalogButton instance.
        /// </summary>
        /// Value range is 0 to 1.
        /// Value if analog stick is held all the way downward is 1. Note that 
        /// the value is positive, which is the opposite of the AnalogStick's Position.Y.
        /// Value if analog stick is in neutral position is 0.
        /// Value if analog stick is held all the way upward is 0.
        public AnalogButton DownAsButton
        {
            get { return downAsButton; }
        }

        /// <summary>
        /// Returns the angle of the analog stick in radians.  
        /// 0 is to the right, Pi/2 is up, Pi is to the left, and 3*Pi/2 is down.
        /// If the analog stick's position is (0,0), the Angle returned is 0 (to the right).
        /// </summary>
        public double Angle => mAngle;

        /// <summary>
        /// Gets the distance from the center position of the analog stick. 
        /// Value is between 0 and 1, where 0 is the neutral position.
        /// </summary>
        public double Magnitude => mMagnitude; 
        

        #region XML Docs
        /// <summary>
        /// The position of the analog stick.  The range for each component is -1 to 1.
        /// </summary>
        #endregion
        public Vector2 Position
        {
            get { return mPosition; }
        }


        public Vector2 Velocity
        {
            get { return mVelocity; }
        }


        #endregion

        #region Methods

        public AnalogStick()
        {
            leftAsButton = new AnalogButton();
            rightAsButton = new AnalogButton();
            upAsButton = new AnalogButton();
            downAsButton = new AnalogButton();
        }

        public bool AsDPadDown(Xbox360GamePad.DPadDirection direction)
        {
            switch (direction)
            {
                case Xbox360GamePad.DPadDirection.Left:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Left])
                    {
                        return mPosition.X < -DPadOffValue;
                    }
                    else
                    {
                        return mPosition.X < -DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Right:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Right])
                    {
                        return mPosition.X > DPadOffValue;
                    }
                    else
                    {
                        return mPosition.X > DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Up:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Up])
                    {
                        return mPosition.Y > DPadOffValue;
                    }
                    else
                    {
                        return mPosition.Y > DPadOnValue;
                    }

                    //break;

                case Xbox360GamePad.DPadDirection.Down:

                    if (mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Down])
                    {
                        return mPosition.Y < -DPadOffValue;
                    }
                    else
                    {
                        return mPosition.Y < -DPadOnValue;
                    }

                    //break;
                default:
                    
                    return false;
                    //break;
            }
        }

        public bool AsDPadPushed(Xbox360GamePad.DPadDirection direction)
        {
            // If the last was not down and this one is, then report a push.
            return mLastDPadDown[(int)direction] == false && AsDPadDown(direction);
        }

        public bool AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection direction)
        {
            // Ignoring is performed inside this call.
            return AsDPadPushedRepeatRate(direction, .35, .12);
        }


        public bool AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection direction, double timeAfterPush, double timeBetweenRepeating)
        {
            if (AsDPadPushed(direction))
                return true;

            // If this method is called multiple times per frame this line
            // of code guarantees that the user will get true every time until
            // the next TimeManager.Update (next frame).
            bool repeatedThisFrame = mLastDPadPush[(int)direction] == TimeManager.CurrentTime;

            if (repeatedThisFrame ||
                (
                AsDPadDown(direction) &&
                TimeManager.CurrentTime - mLastDPadPush[(int)direction] > timeAfterPush &&
                TimeManager.CurrentTime - mLastDPadRepeatRate[(int)direction] > timeBetweenRepeating)
                )
            {
                mLastDPadRepeatRate[(int)direction] = TimeManager.CurrentTime;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            mPosition.X = mPosition.Y = 0;
            mVelocity.X = mVelocity.Y = 0;
            mAngle = 0;
        }

        public override string ToString()
        {
            return "Position" + mPosition.ToString();
        }

        public void Update(Vector2 newPosition)
        {
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Up] = AsDPadDown(Xbox360GamePad.DPadDirection.Up);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Down] = AsDPadDown(Xbox360GamePad.DPadDirection.Down);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Left] = AsDPadDown(Xbox360GamePad.DPadDirection.Left);
            mLastDPadDown[(int)Xbox360GamePad.DPadDirection.Right] = AsDPadDown(Xbox360GamePad.DPadDirection.Right);

            mVelocity = Vector2.Multiply((newPosition - mPosition), 1/TimeManager.SecondDifference);
            mPosition = newPosition;

            UpdateAccordingToPosition();

            for (int i = 0; i < 4; i++)
            {
                if (AsDPadPushed((Xbox360GamePad.DPadDirection)i))
                {
                    mLastDPadPush[i] = TimeManager.CurrentTime;
                }
            }

            leftAsButton.Update(-System.Math.Min(0, mPosition.X));
            rightAsButton.Update(System.Math.Max(0, mPosition.X));

            downAsButton.Update(-System.Math.Min(0, mPosition.Y));
            upAsButton.Update(System.Math.Max(0, mPosition.Y));
        }

        private void UpdateAccordingToPosition()
        {
            // Atan2 of (0,0) returns 0
            mAngle = System.Math.Atan2(mPosition.Y, mPosition.X);
            mAngle = MathFunctions.RegulateAngle(mAngle);
            mMagnitude = System.Math.Min(1, mPosition.Length());
        }

        #endregion

        #region I2DInput Implementation

        float I2DInput.X
        {
            get { return Position.X; }
        }

        float I2DInput.Y
        {
            get { return Position.Y; }
        }

        float I2DInput.XVelocity
        {
            get { return Velocity.X; }
        }

        float I2DInput.YVelocity
        {
            get { return Velocity.Y; }
        }


        float I2DInput.Magnitude => (float) this.mMagnitude;

        #endregion

        I1DInput horizontal;
        public I1DInput Horizontal
        {
            get
            {
                if (horizontal == null)
                {
                    horizontal = new AnalogStickHorizontal(this);
                }

                return horizontal;
            }
        }

        I1DInput vertical;
        public I1DInput Vertical
        {
            get
            {
                if(vertical == null)
                {
                    vertical = new AnalogStickVertical(this);
                }

                return vertical;
            }
        }


    }
}

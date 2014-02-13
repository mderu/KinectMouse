using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Forms;

namespace KinectMouse
{
    class Program
    {
        public static KinectSensor kinect = null;
        public static Skeleton[] skeletons;
        public static int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        public static int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        public static double sensitivity = 1.5;
        //Cursor is the point at which the dominant hand is located. 
        //The mouse's position gets set to this point.
        public static System.Drawing.Point cursor;

        /* Here we save the "player" skeleton, the current one being dominantly tracked by the Kinect.
         * Player is selected by being the first skeleton arbritrarily tracked by the kinect. It then
         * stays as this person until (s)he leaves the sensor's range, or something eclipses them from view.
         * Player will then switch to the next person being tracked. Tracking people only occurs at "near"
         * range from the kinect, so accidently picking up background persons should not be an issue.
         * */
        public static Skeleton player = null;
        //Assume the player is right handed. More on this later.
        public static JointType dominantHand = JointType.HandRight;
        //This is the index of the current player.
        public static int curSkeleton = 0;

        static void Main(string[] args)
        {
            cursor = new System.Drawing.Point(0, 0);
            //This is the code to connect to the kinect. It waits for the Kinect to be plugged in to start the mouse.
            while (kinect == null)
            {
                kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
                if (kinect == null)
                {
                    Console.WriteLine("Kinect not found. Please connect your Kinect to the computer and press enter.");
                    if(Console.ReadLine() == "q")
                    {
                        Environment.Exit(0);
                    }
                }
            }
            //Enables skeletons to be read, and creates a global skeletons variable to prevent constant memory allocations.
            kinect.SkeletonStream.Enable();
            skeletons = new Skeleton[kinect.SkeletonStream.FrameSkeletonArrayLength];
            kinect.SkeletonFrameReady += kinect_SkeletonFrameReady;
            kinect.DepthStream.Range = DepthRange.Near;
            kinect.SkeletonStream.EnableTrackingInNearRange = true;
            kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;//Standing Mode

            //Sets how smooth the Kinect senses the joints of the player,
            //and effectively, the mouse.
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.1f,
                Prediction = 0.5f,
                JitterRadius = 0.1f,
                MaxDeviationRadius = 0.1f
            };
            kinect.SkeletonStream.Enable(parameters);

            
            kinect.Start();
            Console.ReadLine();
        }


        public static void kinect_SkeletonFrameReady (object sender, SkeletonFrameReadyEventArgs e) {
            //Honestly, I dont know. I didn't know the using keyword existed outside of #include
            using (SkeletonFrame SFrame = e.OpenSkeletonFrame())
            {
                //If skeletonOpenFrame returned something
                if (SFrame != null)
                {
                    //Set the skeletons to contain the array of skeletons from SFrame
                    skeletons = new Skeleton[SFrame.SkeletonArrayLength];
                    SFrame.CopySkeletonDataTo(skeletons);

                    
                    player = null;
                    //If player is no longer on screen
                    if (skeletons.ElementAt(curSkeleton).TrackingState != SkeletonTrackingState.Tracked)
                    {
                        //Here we select a new player to pick as the one controlling the mouse. The "skeletons"
                        //variable contains skeletons at completely arbitrary indexes, so we have to go through
                        //all the elements to find one being tracked.
                        for (int i = 0; i < skeletons.Count(); i++)
                        {
                            if (skeletons.ElementAt(i).TrackingState == SkeletonTrackingState.Tracked)
                            {
                                curSkeleton = i;
                                player = skeletons.ElementAt(curSkeleton);
                                break;
                            }
                        }
                    }
                    else
                    {
                        player = skeletons.ElementAt(curSkeleton);
                    }
                    
                    //If there is at least one person being tracked:
                    if (player != null)
                    {
                        

                        //If the player's dominant hand is below the screen
                        if ( ( .5 - player.Joints[dominantHand].Position.Y ) * screenHeight * sensitivity > screenHeight)
                        {
                            //Switch to the player's other hand.
                            JointType offHand = dominantHand == JointType.HandRight ? JointType.HandLeft : JointType.HandRight;
                            //If the offHand is NOT below the screen, switch hands.
                            if ((.5 - player.Joints[offHand].Position.Y) * screenHeight * sensitivity <= screenHeight)
                            {
                                dominantHand = offHand;
                            }
                        }
                        cursor.X = (int)((player.Joints[dominantHand].Position.X + .5) * screenWidth * sensitivity);
                        cursor.Y = (int)((.5 - player.Joints[dominantHand].Position.Y) * screenHeight * sensitivity);

                        System.Windows.Forms.Cursor.Position = cursor;

                        Console.WriteLine( cursor.X + ", " + cursor.Y );
                    }
                    else
                    {
                        //Console.WriteLine("Nobody Tracked.");
                    }
                    
                }
            }
        }
    }
}

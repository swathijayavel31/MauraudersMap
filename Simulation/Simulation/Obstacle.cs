using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class Obstacle
    {
        // PUBLIC CONSTANTS
        public enum states { ACTIVE = 0, REMOVE }

        // PRIVATE FIELDS
        private int id;
        private string name;
        private states state;
        private List<Tuple<double, double>> points;
        
        // PUBLIC PROPERTIES
        public int Id { get { return id; } }
        public string Name { get { return String.Copy(name); } set { name = String.Copy(value); } }
        public states State { get { return state; } set { state = value; } }
        public List<Tuple<double, double>> Points { get { return points; } }  // TODO:  Remove after test

        public Obstacle() {}

        public Obstacle(int id, string name, List<Tuple<double, double>> points)
        {
            this.id = id;
            this.name = String.Copy(name);
            state = states.ACTIVE;
            this.points = points;
        }

        public void append_point(double lat, double lng)
        {
            points.Add(new Tuple<double, double>(lat, lng));
        }

        public void append_points(List<Tuple<double, double>> points)
        {
            points.AddRange(points);
        }

        public Tuple<double, double> remove_last_point()
        {
            if (points.Count > 0) {
                Tuple<double, double> last_point = points[points.Count - 1];
                points.RemoveAt(points.Count - 1);
                return last_point;
            }
            else return null;
        }

        public void remove_all_points()
        {
            points.Clear();
        }

        public Tuple<double, double> find_intersection(double lat0, double lng0, double lat1, double lng1)
        {
            if (points.Count < 2) return null;
            else
            {
                Tuple<double, double> intersect_point = null;
                double b_lat0 = points[0].Item1;
                double b_lng0 = points[0].Item2;
                bool intersect_found = false;
                int i = 1;
                while (!intersect_found && i<points.Count)
                {

                    double b_lat1 = points[i].Item1;
                    double b_lng1 = points[i].Item2;

                    // Handle two parallel vertical lines
                    if (lng0==lng1 && b_lng0==b_lng1) {
                        if (lng0==b_lng0) {
                            // Segments on same vertical line
                            // Determine if segments overlap
                            double y_top = Math.Max(lat0, lat1);
                            double y_bottom = Math.Min(lat0, lat1);
                            double y_top_b = Math.Max(b_lat0, b_lat1);
                            double y_bottom_b = Math.Min(b_lat0, b_lat1);
                            if (y_top>=y_bottom_b) intersect_point = new Tuple<double, double>(lng0,y_top);
                            else if (y_top_b>=y_bottom) intersect_point = new Tuple<double,double>(lng0,y_top_b);
                        }
                    }
                    else if (lng0==lng1) {
                        // Input line vertical
                        // solve for y of intersection
                        double b_m = (b_lat1-b_lat0)/(b_lng1-b_lng0);
                        double b_b = b_lat0 - b_m*b_lng0;
                        double y = b_m*lng0 + b_b;
                        intersect_point = new Tuple<double, double>(lng0,y);
                    }
                    else if (b_lng0==b_lng1) {
                        // Obstacle line vertical
                        // solve for y of intersection
                        double m = (lat1-lat0)/(lng1-lng0);
                        double b = lat0 - m*lng0;
                        double y = m*b_lng0 + b;
                        intersect_point = new Tuple<double, double>(b_lng0,y);
                    }

                    else {
                        // Both lines not vertical
                        // Determine slopes
                        double m = (lat1-lat0)/(lng1-lng0);
                        double b_m = (b_lat1-b_lat0)/(b_lng1-b_lng0);

                        // Determine y intercepts
                        double b = lat0 - m*lng0;
                        double b_b = b_lat0 - b_m*b_lng0;

                        // Solve for x and y of intersection
                        double x = -(b-b_b)/(m-b_m);
                        double y = m*x + b;

                        intersect_point = new Tuple<double,double>(x, y);
                    }

                    if (intersect_point!=null) {
                        // Determine if intersect point is in valid range
                        double x_left = Math.Min(lng0, lng1);
                        double x_right = Math.Max(lng0, lng1);
                        double y_top = Math.Max(lat0, lat1);
                        double y_bottom = Math.Min(lat0, lat1);
                        double x_left_b = Math.Min(b_lng0, b_lng1);
                        double x_right_b = Math.Max(b_lng0, b_lng1);
                        double y_top_b = Math.Max(b_lat0, b_lat1);
                        double y_bottom_b = Math.Min(b_lat0, b_lat1);

                        double x = intersect_point.Item1;
                        double y = intersect_point.Item2;

                        if (x>=x_left && x<=x_right && y>=y_bottom && y<=y_top &&
                            x>=x_left_b && x<=x_right_b && y>=y_bottom_b && y<=y_top_b) {
                                intersect_found = true;
                        }
                        else intersect_point = null;
                    }

                    // Update variables
                    b_lat0 = b_lat1;
                    b_lng0 = b_lng1;
                    i++;
                }
                return intersect_point;
            }
        }
    }
}
namespace Knightworld.Core
{
    public static class RailroadMaps
    {
        public const string TheLocal =
@"title The Local
start millhaven
speed 6
minhop 20

town millhaven Millhaven
town lakeside Lakeside
town hillcrest Hillcrest
town emberford Emberford
town portmere Portmere
town willowgate Willowgate
town saltmarsh Saltmarsh
town copsewood Copsewood
town northspire Northspire
town stonebridge Stonebridge

track millhaven lakeside 12
track millhaven portmere 8
track millhaven willowgate 8
track lakeside hillcrest 9
track lakeside emberford 12
track lakeside saltmarsh 8
track lakeside copsewood 9
track hillcrest emberford 8
track hillcrest copsewood 6
track hillcrest northspire 9
track emberford portmere 9
track emberford stonebridge 10
track emberford northspire 8
track portmere willowgate 8
track portmere stonebridge 8
track saltmarsh copsewood 10
track northspire stonebridge 13

landmark lake lakeside
landmark marsh saltmarsh
";
    }
}

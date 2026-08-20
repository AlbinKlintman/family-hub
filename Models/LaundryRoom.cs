namespace WebApp.Models;

public enum LaundryRoom
{
    Room1Left,
    Room2Right
}

public static class LaundryRoomExtensions
{
    public static string ToDisplayName(this LaundryRoom room) => room switch
    {
        LaundryRoom.Room1Left => "Room 1 (Left)",
        LaundryRoom.Room2Right => "Room 2 (Right)",
        _ => room.ToString()
    };
}

namespace SeatTooLong.Core;

public interface IPersonDetector
{
    bool DetectPerson(byte[] frameData, int width, int height);
}

using MathPet.Core;
namespace MathPet.UI;

public interface ITeachingVideoPlayer
{
    Task OpenAsync(CourseVideo video);
}

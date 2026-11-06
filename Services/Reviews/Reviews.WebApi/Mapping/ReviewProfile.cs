using AutoMapper;
using MongoDB.Bson;
using Reviews.Application.DTOs;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.WebApi.Mapping;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        // Custom type converter for ObjectId <-> string (Criterion 12)
        CreateMap<ObjectId, string>().ConvertUsing(oid => oid.ToString());
        CreateMap<string, ObjectId>().ConvertUsing(str => ObjectId.Parse(str));

        CreateMap<Author, AuthorDto>();
        CreateMap<Comment, CommentDto>();

        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.DiscussionId, opt => opt.MapFrom(s => s.DiscussionId.HasValue ? s.DiscussionId.Value.ToString() : null))
            .ForMember(d => d.Comments, opt => opt.MapFrom(s => s.Comments))
            .ForMember(d => d.Photos, opt => opt.MapFrom(s => s.Photos));
    }
}

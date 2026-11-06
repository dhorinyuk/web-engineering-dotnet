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
        CreateMap<ObjectId, string>().ConvertUsing(oid => oid.ToString());
        CreateMap<string, ObjectId>().ConvertUsing(str => ObjectId.Parse(str));

        CreateMap<Author, AuthorDto>();
        CreateMap<Comment, CommentDto>();

        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Author.DisplayName))
            .ForMember(d => d.CommentsCount, o => o.MapFrom(s => s.Comments.Count));
    }
}

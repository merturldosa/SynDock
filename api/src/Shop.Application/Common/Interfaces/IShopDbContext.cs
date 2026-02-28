using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IShopDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Review> Reviews { get; }
    DbSet<QnA> QnAs { get; }
    DbSet<QnAReply> QnAReplies { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<Page> Pages { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostImage> PostImages { get; }
    DbSet<PostComment> PostComments { get; }
    DbSet<PostReaction> PostReactions { get; }
    DbSet<Follow> Follows { get; }
    DbSet<Hashtag> Hashtags { get; }
    DbSet<PostHashtag> PostHashtags { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<UserCoupon> UserCoupons { get; }
    DbSet<UserPoint> UserPoints { get; }
    DbSet<PointHistory> PointHistories { get; }
    DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

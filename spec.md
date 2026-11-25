# Feature Request: Order Management

We need to add order-based operations to the warehouse service. Right now, stock can only be reserved, retrieved, and released for individual articles. We want to support multi-article orders so that a customer can reserve stock for several articles at once as part of a single order.

## What we need

Three new operations:

1. **Reserve an order**: reserve stock for multiple articles in one go. If any article can't be reserved (doesn't exist, not enough stock), the entire order should fail and nothing should change.
2. **Fulfill a reserved order**: physically remove the reserved stock from the warehouse. This should only work for orders that were previously reserved.
3. **Cancel a reserved order**: release the reserved stock so it becomes available again. This should only work for orders that were previously reserved.

Orders should be tracked so that a reserved order can later be fulfilled or cancelled.

## Additional context

- Look at how the existing single-article operations work (reserve, retrieve, release) for patterns to follow.
- The service already publishes events for stock changes. New operations should do the same.
- All quantities in an order must be positive.
- An order must contain at least one article.

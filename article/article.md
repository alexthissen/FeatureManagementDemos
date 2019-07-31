# Feature Management

Your DevOps team wants to deliver business value as often and as fast as possible. Ideally you want to release a new feature whenever it seems ready. With feature flags your can achieve this selective releasing. But how do you do this in a structural and maintainable way. We will try to explain that in this article.

## Feature Flags
Feature flags are very simple. In essence they are just blocks of code/functionality wrapped in a if/else block. Then there is usually some sort of management interface that allows enabling and disabling that certain if/else block. That way you can enabling or disable functionality at the moment you choose. This moment is completely separate of the actually deployment of the software. 




# Locality Sensitive Hashing

While the LSH algorithm is relatively simple, determining the parameters for it is tricky. This library contains both an implementation of LSH and the utilities to determine the optimal parameters, based on [Slaney2012](<Slaney2012(OptimalLSH).pdf>).

## Implementation Style

While care is taken to properly model the mathematical concepts, generalization is kept to a minimum and only the required functionality is implemented. This keeps the code base small and easy to understand. The concepts are documented in this readme instead of the code, due to the superior markup capabilities of Markdown.

## Mathematical Concepts

### Product distribution

[Wikipedia](https://en.wikipedia.org/wiki/Distribution_of_the_product_of_two_random_variables)

If $X $ and $Y $ are two independent, continuous random variables, described by probability density functions $f_X $ and $f_Y $ then the probability density function of $Z = XY$ is

$$f_Z(z) = \int^\infty_{-\infty} f_X(x)  f_Y( z/x)  \frac{1}{|x|}\, dx$$

For our purposes, we only need the multiplication between a unit normal distribution and either a histogram pdf or a unit impulse.

#### Unit Impulse

The pdf of a unit impulse $\delta(x)$ is zero everywhere except at $x=0$ and sums to one: $$1=\int^\infty_{-\infty} \delta(z)  dx $$

For $X\sim\delta(x-v)$, the pdf $f_X(x)$ is non zero only at $v$. Therefore $x$ can be treated as constant with value $v$ in the integral:

$$
\begin{align}
f_Z(z) &= \int^\infty_{-\infty} \delta(x-v)  f_Y( z/x)  \frac{1}{|x|}\, dx \\
&=  f_Y( z/v)  \frac{1}{|v|} \int^\infty_{-\infty} \delta(x-v) \, dx \\
&= f_Y( z/v)  \frac{1}{|v|}
\end{align}
$$

The probability density function of the normal distribution is

$$f_N(x|\mu,\sigma^2)= \frac{1}{\sqrt{2 \pi \sigma^2}} e^{-\frac{(x-\mu)^2}{2 \sigma^2}} $$

If we plug the formula for the unit normal distribution ($\mu=0$, $\sigma = 1$) into our equation above, we get

$$
\begin{align}
f_Z(z) &= f_N( z/v)  \frac{1}{|v|}\\
&= \frac{1}{|v|\sqrt{2 \pi}} e^{-\frac{(z/v)^2}{2 }}   \\
&= \frac{1}{\sqrt{2 \pi v^2}} e^{-\frac{z^2}{2v^2 }}   \\
&= f_N(z|0,v^2)
\end{align}
$$

Thus $Z\sim N(0,v^2)$.

#### Histogram PDF

We can treat a histogram PDF as a sum of impulses. With $n$ bins, $B(i)$ and $C(i)$ the probability and center of bin $i$ respectively, we have get

$$
\begin{align}
f_Z(z) &= \sum_{i=1}^n\int^\infty_{-\infty} B(i) \delta(z-C(i))  f_N( z/x)  \frac{1}{|x|}\, dx\\
&=  \sum_{i=1}^n B(i)\int^\infty_{-\infty} \delta(z-C(i))  f_N( z/x)  \frac{1}{|x|}\, dx\\
&=  \sum_{i=1}^n B(i)f_N(z/C(i))\frac{1}{|C(i)|}\\
&=  \sum_{i=1}^n B(i)f_N(z|0,C(i)^2)\\
\end{align}
$$

If $X$ is distributed normally with mean $\mu$ and variance $\sigma^2$, then $ aX \sim N( a \mu, a^2\sigma^2)$. Using this we can further simplify the sum above:

$$
\begin{align}
f_Z(z)&=  \sum_{i=1}^n B(i)f_N(z|0,C(i)^2)\\
&=  \sum_{i=1}^n f_N(z|0,B(i)^2C(i)^2)\\
\end{align}
$$

If $X_1$ and $X_2$ are two independent normal random variables with means $\mu_1$ and $\mu_2$, and variances $\sigma^2_1$ and $\sigma^2_2$. Then their sum $X_1 + X_2 $ will also be normally distributed, with mean $\mu_1 + \mu_2$ and variance $\sigma^2_1 + \sigma^2_2$. Therefore we get

$$
\begin{align}
f_Z(z) &= f_N(z | 0, \sum_{i=1}^n B(i)^2C(i)^2)
\end{align}
$$

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

We can treat a histogram PDF as a sum of impulses. With $n$ bins and $B_i$ and $C_i$ as the probability and center of bin $i$ respectively, we get

$$
\begin{align}
f_Z(z) &= \sum_{i=1}^n\int^\infty_{-\infty} B_i \delta(z-C_i)  f_N( z/x)  \frac{1}{|x|}\, dx\\
&=  \sum_{i=1}^n B_i\int^\infty_{-\infty} \delta(z-C_i)  f_N( z/x)  \frac{1}{|x|}\, dx\\
&=  \sum_{i=1}^n B_if_N(z/C_i)\frac{1}{|C_i|}\\
&=  \sum_{i=1}^n B_if_N(z|0,C_i^2)\\
\end{align}
$$

From [Stack Overflow](https://stats.stackexchange.com/questions/205126/standard-deviation-for-weighted-sum-of-normal-distributions):

Two normally distributed random variable $H_0$ and $H_1$, which are combined to give the weighted distribution $H$ as follows:

$$
\begin{align}
H_0 &\sim N(\mu_0, \sigma_0)\\
H_1 &\sim N(\mu_1, \sigma_1)\\
f_H &= p * f_1(x) + (1-p)  f_0(x)
\end{align}
$$

The random variable $H$ is the mixture of two normal distributions. For the mean of $H$

$$E(H) = \int x\left(pf_1(x) + (1-p)f_0(x) \right) dx = p\mu_1 + (1-p)\mu_0. $$

Similarly for the second moment of H

$$
\begin{align*}
E(H^2)& = \int x^2 \left(pf_1(x) + (1-p)f_0(x) \right) dx\\
& = pE(H_1^2) + (1-p)E(H_0^2)\\
& = p(\sigma_1^2 + \mu_1^2) + (1-p)(\sigma_0^2 + \mu_0^2)
\end{align*}
$$

Finally,

$$
\begin{align*}
Var(H) & = E(H^2) - [E(H)]^2\\
& = p(\sigma_1^2 + \mu_1^2) + (1-p)(\sigma_0^2 + \mu_0^2) - \left[p\mu_1 + (1-p)\mu_0 \right]^2\\
& = \left[p\sigma_1^2 + (1-p)\sigma_0^2\right] + [p\mu_1^2 + (1-p)\mu_0^2]- \left[p\mu_1 + (1-p)\mu_0 \right]^2\\
& = p\sigma^2_1+(1−p)\sigma^2_0+p(1−p)(\mu_1−\mu_0)^2
\end{align*}
$$

Taking the square root of $Var(H)$, you get the standard deviation.

In our case, $\mu_0$ and $\mu_1$ are both zero, thus $\sigma^2= p\sigma^2_1+(1−p)\sigma^2_0$. Using this we can further simplify the sum above:

$$
\begin{align}
f_Z(z)&=  \sum_{i=1}^n B_if_N(z|0,C_i^2)\\
&=  f_N(z|0,\sum_{i=1}^n (B_iC_i^2))\\
\end{align}
$$

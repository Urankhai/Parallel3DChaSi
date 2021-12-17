d = 0:0.01:100;
D0 = 80;

asd = 0.5*(sin(pi*(D0 - d)/D0 - pi/2)+1);
figure
plot(d,asd)
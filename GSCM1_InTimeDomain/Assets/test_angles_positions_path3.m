close all
mpc1 = [-80.93064, 1.680033, 5.992523];
mpc2 = [-82.98604, 1.498653, -12.63292];
nrm2 = [0.7741253, 0.0, 0.6330324];
nrm2 = nrm2/norm(nrm2);
prp2 = [nrm2(3), 0, -nrm2(1)];
mpc3 = [-45.51039, 2.087998, 5.378403];

nline2 = [mpc2(1), mpc2(3);mpc2(1)+5*nrm2(1),mpc2(3)+5*nrm2(3)];
pline2 = [mpc2(1), mpc2(3);mpc2(1)+5*prp2(1),mpc2(3)+5*prp2(3)];

sgmnt1 = [mpc1(1), mpc1(3);mpc2(1),mpc2(3)];
sgmnt2 = [mpc2(1), mpc2(3);mpc3(1),mpc3(3)];

b = (mpc2-mpc1)/norm(mpc2-mpc1);
a = (mpc3-mpc2)/norm(mpc3-mpc2);

Y1 = -b*prp2';
X1 = b*nrm2';

Y2 = a*prp2';
X2 = a*nrm2';

theta1 = 180*atan2(Y1,X1)/pi
theta2 = 180*atan2(Y2,X2)/pi

figure
axis equal
hold on
plot(mpc1(1),mpc1(3),'o')
plot(mpc2(1),mpc2(3),'v')
plot(nline2(:,1),nline2(:,2))
plot(pline2(:,1),pline2(:,2))
plot(mpc3(1),mpc3(3),'s')

plot(sgmnt1(:,1),sgmnt1(:,2),'b')
plot(sgmnt2(:,1),sgmnt2(:,2),'r')
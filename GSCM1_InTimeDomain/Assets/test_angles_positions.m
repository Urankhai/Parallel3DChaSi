clear all
close all

car1 = [125.4, 1.7, -20.0];
car2 = [177.0, 1.8, -26.0];

mpc1 = [73.8, 1.5, -8.8];
mpc2 = [23.1, 1.9, -26.6];

per1 = [1.0, 0.0, -0.1];
per2 = [0.1, 0.0, 1.0];

nor1 = [-0.1, 0.0, -1.0];
nor2 = [1.0, 0.0, -0.1];

% from car1 to mpc1
dir11 = [-1.0, 0.0, 0.2];
% from mpc2 to car2
dir22 = [1.0, 0.0, 0.0];

nor1 = nor1/norm(nor1);
per1 = per1/norm(per1);
nline1 = [mpc1(1),mpc1(3);mpc1(1)+5*nor1(1),mpc1(3)+5*nor1(3)];
pline1 = [mpc1(1),mpc1(3);mpc1(1)+5*per1(1),mpc1(3)+5*per1(3)];
dir11 = dir11/norm(dir11);

nor2 = nor2/norm(nor2);
per2 = per2/norm(per2);
nline2 = [mpc2(1),mpc2(3);mpc2(1)+5*nor2(1),mpc2(3)+5*nor2(3)];
pline2 = [mpc2(1),mpc2(3);mpc2(1)+5*per2(1),mpc2(3)+5*per2(3)];
dir22 = dir22/norm(dir22);

segm1 = [car1(1),car1(3);mpc1(1),mpc1(3)];
segm2 = [mpc1(1),mpc1(3);mpc2(1),mpc2(3)];
segm3 = [mpc2(1),mpc2(3);car2(1),car2(3)];

figure
hold on
axis equal
plot(car1(1),car1(3), 'o')
plot(mpc1(1),mpc1(3), '^')
plot(car2(1),car2(3), '*')
plot(mpc2(1),mpc2(3), 's')

plot(segm1(:,1),segm1(:,2),'c')
plot(segm2(:,1),segm2(:,2),'b')
plot(segm3(:,1),segm3(:,2),'g')

plot(pline1(:,1),pline1(:,2),'r')
plot(pline2(:,1),pline2(:,2),'r')
plot(nline1(:,1),nline1(:,2),'m')
plot(nline2(:,1),nline2(:,2),'m')

%%
mpc1 = [-10.3, 1.9, -19.1];
mpc2 = [-14.7, 2.0, 1.6];
mpc3 = [16.4, 1.2, -20.8];

segm1 = [mpc1(1),mpc1(3); mpc2(1),mpc2(3)];
segm2 = [mpc2(1),mpc2(3); mpc3(1),mpc3(3)];
figure
axis equal
hold on
plot(mpc1(1),mpc1(3),'o')
plot(mpc2(1),mpc2(3),'*')
plot(mpc3(1),mpc3(3),'s')

plot(segm1(:,1),segm1(:,2),'r')
plot(segm2(:,1),segm2(:,2),'m')
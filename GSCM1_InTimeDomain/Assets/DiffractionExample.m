%*********************************************************************
%***Program coded by: Danda B. Rawat
%***Department: ECE, ODU, Norfolk, VA, USA
%***Date: 02/06/2007
%***Modified: 02/06/2007
%***Email: drawaat@gmail.com
%*********************************************************************
%***Assignment Number 1: Problem Number 3(1)
%***This program calculate the attenuation factor introduced by Knife-edge 
%***diffraction using complex Fresnel integral.
%**************************************************************************

close all
clear all

mue=-5:5; % value of mue
inde=0; 
for vmuer=-5:5
inde=inde+1;
intFe=quad('exp((-j*pi*x.^2)/2)',vmuer,20); %Integration of the function used in integral part of Complex Fresnel Integral
fe=abs((0.5+0.5*1j)*intFe ); %Complex Fresnel Integral.
Gdb_e(inde)=20*log10(fe); %attenuation factor introduced by Knife-edge diffraction
end

%*********************************************************************
%***Assignment Number 1: Problem Number 3(2)
%***This program calculate and plot the approximate attenuation factor introduced by Knife-edge 
%***diffraction using complex Fresnel integral.
%**************************************************************************

i=0;
LL=-5;
UL=5;
v=LL:UL;
for vn=LL:UL
i=i+1;

if(vn < -1.0)
	Gdb(i)=0;
elseif( vn <= 0)
	Gdb(i)=20*log10(0.5-0.62*vn);
elseif(vn <= 1)
	Gdb(i)=20*log10(0.5*exp(-0.95*vn));
elseif(vn <= 2.4)
	Gdb(i)=20*log10(0.4-sqrt(0.1184-(0.38-0.1*vn).^2));
else
	Gdb(i)=20*log10(0.225/vn);
end
end

figure
plot(v,Gdb,mue,Gdb_e)
xlabel('x (Freshnel-Diferential Parameter V)'); 
ylabel('y (Knife-Edge Differential Gain Gd[dB])'); 
title('Integrated Diffraction Gain Plot '); 
% gtext('Plot with approximation') 
% gtext('Plot without approximation') 
grid on

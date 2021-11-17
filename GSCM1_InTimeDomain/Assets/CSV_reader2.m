clear all
close all

Hyy = csvread("C:\Users\al6235fe\Work Folders\Desktop\Programs\From_Aleksei\ReadinCSVFiles\H_freq3.csv");

Nfft = 1024;
dt = 1/(Nfft*1000000);
distance2 = 3e8*(0:dt:(Nfft-1)*dt);

tt2 = 0.05:0.05:size(Hyy,1)*0.05;


hyy=fliplr(ifft(Hyy,Nfft,2))*sqrt(Nfft);
PDPyy = zeros(size(Hyy,1),Nfft);

for i = 1:size(Hyy,1)
    PDPyy(i,:) = abs(hyy(i,:)).^2;
end

figure
h=pcolor(distance2(1:800), tt2(40:end), 10*log10(PDPyy(40:end,1:800)));
set(h,'linestyle','none')
caxis([-90 -30])
title('PDP')
xlabel('Delay distance (m)')
ylabel('Time (s)')



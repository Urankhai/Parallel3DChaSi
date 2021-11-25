clear all
% close all
pathname = "C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\H_freq";
colors = {'m','b','g','c','k'};
time_step = 0.02;
Nf = 10; % Number_of_files

PDPavg = zeros(378,1024);
for k = 1:Nf
    fileAddress = pathname + k + ".csv";
    Hload = csvread(fileAddress);
    Hyy = Hload(1:378,:);
    
    Nfft = 1024;
    dt = 1/(Nfft*1000000);
    distance2 = 3e8*(0:dt:(Nfft-1)*dt);
    
    tt2 = time_step:time_step:size(Hyy,1)*time_step;
    
%     disp(size(Hyy,1));
    
    hyy=fliplr(ifft(Hyy,Nfft,2));%*sqrt(Nfft);
    PDPyy = zeros(size(Hyy,1),Nfft);
    
    for i = 1:size(Hyy,1)
        PDPyy(i,:) = abs(hyy(i,:)).^2;
        PDPyy(PDPyy<10^(-115/10))=0;
        PDPyy(:,750:end)=0;
        gyy(i,k)=sum(PDPyy(i,:));
    end
    
    PDPavg = PDPavg + PDPyy;
    
%     figure
%     h=pcolor(distance2(1:800), tt2(40:end), 10*log10(PDPyy(40:end,1:800)));
%     set(h,'linestyle','none')
%     caxis([-115 -70])
%     title('PDP')
%     xlabel('Delay distance (m)')
%     ylabel('Time (s)')
    
    
%     figure(2)
%     hold on
%     plot(7.58*tt2/max(tt2),10*log10(gyy(:,k)).')
%     grid on
end
gsort = sort(gyy,2);

for uu=1:length(gsort(:,end))-10
    gsort1(uu)=mean(10*log10(gsort(uu:uu+10,1)).');
    gsort2(uu)=mean(10*log10(gsort(uu:uu+10,end)).');
end

gavg = sum(gyy,2)/Nf;
figure(2)
plot(7.58*tt2/max(tt2),10*log10(gavg).','r','linewidth',2)

plot(7.58*tt2(6:end-5)/max(tt2),gsort1,'--','color',[0.5 0.5 0.5])
plot(7.58*tt2(6:end-5)/max(tt2),gsort2,'--','color',[0.5 0.5 0.5])


figure
h=pcolor(distance2(1:800), tt2(40:end), 10*log10(PDPavg(40:end,1:800)/Nf));
set(h,'linestyle','none')
caxis([-115 -70])
title('PDP')
xlabel('Delay distance (m)')
ylabel('Time (s)')
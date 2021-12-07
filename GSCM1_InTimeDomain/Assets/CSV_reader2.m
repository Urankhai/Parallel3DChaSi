% clear all

plot_factor = -133;
filt_window = 4;
% close all
pathname = "C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\H_freq";
colors = {'m','b','g','c','k'};
time_step = 0.02;
Nf = 7; % Number_of_files

NumbOfSamples = 378;

Nfft = 1000;
dt = 1/(Nfft*1000000);
distance2 = 3e8*(dt:dt:Nfft*dt);
    


PDPavg = zeros(NumbOfSamples,filt_window*Nfft);

tt2 = time_step:time_step:NumbOfSamples*time_step;


H_long = zeros(NumbOfSamples, filt_window*Nfft);

for k = 1:Nf
    fileAddress = pathname + k + ".csv";
    Hload = csvread(fileAddress);
    Hyy = Hload(1:NumbOfSamples,1:Nfft);
    H_long(:,1:Nfft) = Hyy;
    
%     disp(size(Hyy,1));
    
    hyy=fliplr(ifft(H_long,filt_window*Nfft,2));%*sqrt(Nfft);
    PDPyy = zeros(NumbOfSamples, filt_window*Nfft);
    
    for i = 1:NumbOfSamples
        PDPyy(i,:) = abs(hyy(i,:)).^2;
        PDPyy(PDPyy<10^(plot_factor/10))=0;
        PDPyy(:,filt_window*750:end)=0;
        gyy(i,k)=sum(PDPyy(i,:));
    end
    
    PDPavg = PDPavg + PDPyy;
    disp([' Run # ', num2str(k), '; Total power = ', num2str(10*log10(sum(gyy(:,k))))])
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
% gsort = sort(gyy,2);
% 
% figure
% surf(10*log10(gyy))
% 
% for uu=1:length(gsort(:,end))-10
%     gsort1(uu)=mean(10*log10(gsort(uu:uu+10,1)).');
%     gsort2(uu)=mean(10*log10(gsort(uu:uu+10,end)).');
% end

% gavg = sum(gyy,2)/Nf;
% figure(2)
% plot(7.58*tt2/max(tt2),10*log10(gavg).','r','linewidth',2)
% 
% plot(7.58*tt2(6:end-5)/max(tt2),gsort1,'--','color',[0.5 0.5 0.5])
% plot(7.58*tt2(6:end-5)/max(tt2),gsort2,'--','color',[0.5 0.5 0.5])

%%
time_window = 209;
power_elevation = 7;
figure
h=pcolor(distance2, tt2(1:time_window), 10*log10(PDPavg(end-time_window+1:end,1:filt_window:end)/Nf));
set(h,'linestyle','none')
caxis([plot_factor+power_elevation, -70-power_elevation])
title('PDP')
xlabel('Delay distance (m)')
ylabel('Time (s)')

figure(1)
h=pcolor(3e8*tau,tav(1:end-3),10*log10(PDPnn));
set(h,'linestyle','none')
caxis([-115 -70])
title('PDP')
xlabel('Delay distance (m)')
ylabel('Time (s)')

%%
figure
subplot(1,2,1)
h=pcolor(3e8*tau,tav(1:end-3),10*log10(PDPnn));
set(h,'linestyle','none')
caxis([-115 -70])
title('PDP for GSCM from [3]')
xlabel('Delay distance (m)')
ylabel('Time (s)')

subplot(1,2,2)
h=pcolor(distance2, tt2(1:time_window), 10*log10(PDPavg(end-time_window+1:end,1:filt_window:end)/Nf));
set(h,'linestyle','none')
caxis([plot_factor+power_elevation, -70-power_elevation])
title('PDP from Unity3D Implementation')
xlabel('Delay distance (m)')
ylabel('Time (s)')

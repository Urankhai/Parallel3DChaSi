% clear all
close all


plot_factor = -115;
filt_window = 1;



pathname = 'H_freq'; 
colors = {'m','b','g','c','k'};
time_step = 0.02;
Nf = 2; % Number_of_files

NumbOfSamples = 372;

Nfft = 1000;
dt = 1/(Nfft*1000000);
distance2 = 3e8*(dt:dt:Nfft*dt);
    


PDPavg = zeros(NumbOfSamples,filt_window*Nfft);

tt2 = time_step:time_step:NumbOfSamples*time_step;

gyy = zeros(NumbOfSamples, Nf);
Averaging_ID = zeros(NumbOfSamples, 1);

H_long = zeros(NumbOfSamples, filt_window*Nfft);

figure
view(60,30)
hold on

number_of_files = 0;
for k = 1:Nf%2001:2003%1001:1013
    number_of_files = number_of_files + 1;
    
    kk = k+4000;
    fileAddress = [pathname, num2str(kk),'.csv'];
    Hload = csvread(fileAddress);
    Hyy = Hload(1:NumbOfSamples,1:Nfft);
    H_long(:,1:Nfft) = Hyy;
    
%     disp(size(Hyy,1));
    
    hyy=fliplr(ifft(H_long,filt_window*Nfft,2));%*sqrt(Nfft);
    PDPyy = zeros(NumbOfSamples, filt_window*Nfft);
    
    
    y_axis = ones(NumbOfSamples, 1);
    
    for i = 1:NumbOfSamples
        PDPyy(i,:) = abs(hyy(i,:)).^2;
        PDPyy(PDPyy<10^(plot_factor/10))=0;
        PDPyy(:,filt_window*750:end)=0;
        gyy(i,k)=sum(PDPyy(i,:));
    end
    
    plot3(y_axis*k, tt2, 10*log10(gyy(:,k)))
    PDPavg = PDPavg + PDPyy;
    disp([' Run # ', num2str(k), '; Total power = ', num2str(10*log10(sum(gyy(:,k))))])
    
    
%     figure(2)
%     hold on
%     plot(7.58*tt2/max(tt2),10*log10(gyy(:,k)).')
%     grid on
end
%%
gsort = sort(gyy,2);

for ii =1:NumbOfSamples
    Averaging_ID(ii) = nnz(gyy(ii,:));
end

% figure
% surf(10*log10(gyy))

for uu=1:length(gsort(:,end))-10
    gsort1(uu)=mean(10*log10(gsort(uu:uu+10,1)).');
    gsort2(uu)=mean(10*log10(gsort(uu:uu+10,end)).');
end

gavg = sum(gyy,2)/number_of_files;

data = open('NarrowPDP171_11.fig');
extract = findobj(data,'Type','line');

figure
grid on
hold on
ScalingTime = 7.4;%7.58;
ylim([-115 -65])
plot(ScalingTime*tt2/max(tt2),10*log10(gyy))

plot(ScalingTime*tt2/max(tt2),10*log10(gavg).','r','linewidth',2)

plot(ScalingTime*tt2(6:end-5)/max(tt2),gsort1,'--k', 'linewidth',1)
plot(ScalingTime*tt2(6:end-5)/max(tt2),gsort2,'--k', 'linewidth',1)

for nn = 1:length(extract)
    plot(extract(nn).XData, extract(nn).YData,'k','linewidth', 2); 
end




%%
time_shift = 12;
time_window = 209;
power_elevation = 5;
% figure
% h=pcolor(distance2, tt2(1:time_window), 10*log10(PDPavg(end-time_window+1:end,1:filt_window:end)/Nf));
% set(h,'linestyle','none')
% caxis([plot_factor+power_elevation, -70-power_elevation])
% title('PDP')
% xlabel('Delay distance (m)')
% ylabel('Time (s)')
% 
% figure
% h=pcolor(3e8*tau,tav(1:end-3),10*log10(PDPnn));
% set(h,'linestyle','none')
% caxis([-115 -70])
% title('PDP')
% xlabel('Delay distance (m)')
% ylabel('Time (s)')


figure
subplot(2,1,2)
h=pcolor(3e8*tau,3+tav(1:end-3),10*log10(PDPnn));
set(h,'linestyle','none')
caxis([-115 -70])
xlim([0 200])
ylim([4 7.18])
title('PDP for GSCM from [3]')
xlabel('Delay distance (m)')
ylabel('Time (s)')

subplot(2,1,1)
h=pcolor(distance2, 3+tt2(1:time_window), 10*log10(PDPavg(end-time_window-time_shift+1:end-time_shift,1:filt_window:end)/number_of_files));
set(h,'linestyle','none')
caxis([-115, -70])
xlim([0 200])
ylim([4 7.18])
title('PDP from Unity3D Implementation')
xlabel('Delay distance (m)')
ylabel('Time (s)')
